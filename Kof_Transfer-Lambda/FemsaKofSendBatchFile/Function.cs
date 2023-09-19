using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using FemsaKofSendBatchFile.Models;
using FemsaKofSendBatchFile.Resources;
using FemsaKofSendBatchFile.Validators;
using FemsaKofSendBatchFile.Validators.Abstractions;
using Newtonsoft.Json;
using static FemsaKofSendBatchFile.Constants;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FemsaKofSendBatchFile;

public class Function
{
    private readonly IAmazonS3 _s3Client;
    private readonly HttpClient _httpClient;    
    private readonly string _gravtyLoginUrl;
    private readonly string _gravtyGetSignedUrl;
    private readonly string _apiKey;
    private readonly string _userName;
    private readonly string _password;
    private readonly int _batchId;
    private readonly int _sponsorId;
    private readonly IValidEnvironmentVariables _validEnvironmentVariables;

    public Function()
    {
        _s3Client = new AmazonS3Client();
        _httpClient = new HttpClient();
        
        _gravtyLoginUrl = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GravtyLoginURL")) ? string.Empty : Environment.GetEnvironmentVariable("GravtyLoginURL");
        _gravtyGetSignedUrl = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GravtyGetSignedURL")) ? string.Empty : Environment.GetEnvironmentVariable("GravtyGetSignedURL");      
        _apiKey = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ApiKey")) ? string.Empty : Environment.GetEnvironmentVariable("ApiKey");
        _userName = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UserName")) ? string.Empty : Environment.GetEnvironmentVariable("UserName");
        _password = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Password")) ? string.Empty : Environment.GetEnvironmentVariable("Password");

        _batchId = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BatchId")) ? 1 : Convert.ToInt32(Environment.GetEnvironmentVariable("BatchId"));
        _sponsorId = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SponsorId")) ? 1 : Convert.ToInt32(Environment.GetEnvironmentVariable("SponsorId"));

        _validEnvironmentVariables = new ValidEnvironmentVariables();
    }

   /// <summary>
   /// Esta lambda escucha el evento cuando un objeto es adicionado a un específico Bucket, 
   /// si esto pasa la lambda se conecta a una URL (Proxy Apigee) el cual devuelve una URL firmada,
   /// con la URL firmada se sube el objeto al bucket respectivo.
   /// </summary>
   /// <param name="evnt"></param>
   /// <param name="context"></param>
   /// <returns></returns>
    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var eventRecords = evnt.Records ?? new List<S3Event.S3EventNotificationRecord>();

        foreach (var record in eventRecords)
        {
            var s3Event = record.S3;

            if (s3Event == null)
                continue;

            try
            {
                context.Logger.Log($"s3Event.Bucket.Name: {s3Event.Bucket.Name}");
                context.Logger.Log($"s3Event.Object.Key: {s3Event.Object.Key}");

                if (_validEnvironmentVariables.IsValidEnvironmentVariables(_gravtyLoginUrl, _gravtyGetSignedUrl, _apiKey, _userName, _password))
                {
                    var buckeName = s3Event.Bucket.Name;
                    var contentType = s3Event.Object.Key.ToGetContentType();

                    context.Logger.Log($"s3 contentType: {contentType}");

                    if (contentType.Equals(RequestValidation.InvalidFileExtension))
                        context.Logger.LogError($"El archivo no puede ser transferido dado que el destiono no soporta el tipo de archivo");
                    else
                    {
                        var requestBody = new RequestBody(s3Event.Object.Key, contentType, _sponsorId, _batchId);
                        var requestLogin = new RequestLogin(_userName, _password);

                        context.Logger.Log($"LLamando a la función ProcessS3Event");

                        await ProcessS3Event(requestLogin, requestBody, buckeName, context);
                    }
                }
                context.Logger.Log($"Las variables de ambiente no son las correctas, por favor, validarlas.");
            }
            catch (Exception ex)
            {                                
                context.Logger.LogError($"Error con la lambda {ex.Message}");
                throw;
            }
        }       
    }

    /// <summary>
    /// Procesa el evento S3 recibido.
    /// </summary>
    /// <param name="requestBody"></param>
    /// <param name="bucketName"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task ProcessS3Event(RequestLogin requestLogin, RequestBody requestBody, string bucketName, ILambdaContext context)
    {       
        try
        {
            var token = await LoginGravty(requestLogin, context);

            if(!string.IsNullOrEmpty(token))
            {
                var signedUrl = await GetGravtySignedUrl(requestBody, token, context);

                if (!string.IsNullOrEmpty(signedUrl))
                {
                    context.Logger.Log("La URL firmada recibida es: " + signedUrl);

                    var binaryData = await DownloadS3FileAsync(bucketName, requestBody.FileName, context);

                    await UploadToGravtyAsync(signedUrl, binaryData, requestBody.FileType, context);
                }
                else                
                    context.Logger.LogError($"ProcessS3Event. No se pudo obtener la URL firmada");             
            }
            else            
                context.Logger.LogError($"ProcessS3Event. No se pudo obtener el token de autorización");                                                          
        }
        catch (Exception ex) 
        {
            context.Logger.LogError($"Error en ProcessS3Event: " + ex.Message);
            throw;
        }        
    }

    /// <summary>
    /// Logearse a Gravty para obtener el token de autorización.
    /// </summary>
    /// <param name="requestLogin"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task<string> LoginGravty(RequestLogin requestLogin, ILambdaContext context)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

            var content = new StringContent(JsonConvert.SerializeObject(requestLogin), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_gravtyLoginUrl, content);
            var contentResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var responseLogin = JsonConvert.DeserializeObject<ResponseLogin>(contentResponse);

                if (responseLogin is { } )
                {
                    if (!string.IsNullOrEmpty(responseLogin.Token))
                    {
                        context.Logger.Log($"Se obtuvo el token de authorization: {prefixToken + responseLogin.Token}");
                        return prefixToken + responseLogin.Token;
                    }
                }               
                context.Logger.LogError($"No se pudo obtener el token de autorizacion.  Respuesta: {JsonConvert.DeserializeObject<string>(contentResponse)}");                         
            }
            else
            {
                var responseErrorLogin = JsonConvert.DeserializeObject<ResponseErrorLogin>(contentResponse);

                if (responseErrorLogin is { } )
                    context.Logger.LogError($"Error en LoginGravty. Mensaje: {responseErrorLogin.ErrorDetails.Message} Código: {responseErrorLogin.ErrorDetails.Code}");
                else
                    context.Logger.LogError($"Error en LoginGravty. Respuesta: {JsonConvert.DeserializeObject<string>(contentResponse)}");
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error en LoginGravty: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    // Implementa la lógica para obtener la URL firmada de Gravty   
    /// </summary>
    /// <param name="requestBody"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task<string> GetGravtySignedUrl(RequestBody requestBody, string token, ILambdaContext context)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", token);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

            context.Logger.Log($"fileName: {requestBody.FileName} type: {requestBody.FileType} " +
                $"batch: {requestBody.BatchId} sponsor: {requestBody.SponsorId}");

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");           

            context.Logger.Log($"apiKey: {_apiKey} token: {token} url: {_gravtyGetSignedUrl}" );
            context.Logger.Log($"url: {_gravtyGetSignedUrl}");

            var response = await _httpClient.PostAsync(_gravtyGetSignedUrl, content);
            var contentResponse = await response.Content.ReadAsStringAsync();

            context.Logger.Log($"contentResponse: {contentResponse}");

            if (contentResponse.ToString().ToUpper().Contains(Forbidden))
                context.Logger.LogError($"Problemas para obtener la URL firmada por permisos.");
            else
            {
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = JsonConvert.DeserializeObject<ResponseBody>(contentResponse);

                    if (responseBody is { })
                    {
                        if (!string.IsNullOrEmpty(responseBody.SignedRequest) && Uri.IsWellFormedUriString(responseBody.SignedRequest, UriKind.Absolute))
                            return responseBody.SignedRequest;
                    }

                    context.Logger.LogError($"Problemas para obtener la URL firmada.  Respuesta: {JsonConvert.DeserializeObject<string>(contentResponse)}");
                }
                else
                    context.Logger.LogError($"Problemas para obtener la URL firmada. StatusCode: {response.StatusCode.ToString()} Respuesta: {JsonConvert.DeserializeObject<string>(contentResponse)}");
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error en GetGravtySignedUrl: {ex.Message}");
            throw;
        }     
    }

    /// <summary>
    // Implementa la lógica para descargar el archivo S3 y devuelve los datos binarios   
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private async Task<byte[]> DownloadS3FileAsync(string bucketName, string fileName, ILambdaContext context)
    {
        try
        {
            byte[] binaryData;

            using (var getObjectResponse = await _s3Client.GetObjectAsync(bucketName, fileName))
            {
                binaryData = await getObjectResponse.ToBinaryDataAsync();
            }
            return binaryData;
        }
        catch (Exception ex) 
        {
            context.Logger.LogError($"Error en DownloadS3FileAsync: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Implementa la lógica para enviar el archivo a Gravty y devuelve un indicador de éxito
    /// </summary>
    /// <param name="gravtyUrl"></param>
    /// <param name="binaryData"></param>
    /// <param name="contentType"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task UploadToGravtyAsync(string gravtyUrl, byte[] binaryData, string contentType, ILambdaContext context)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            ByteArrayContent byteArrayContent = new ByteArrayContent(binaryData);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
           
            var response = await _httpClient.PutAsync(gravtyUrl, byteArrayContent);
            var contentResponse = await response.Content.ReadAsStringAsync();

            // Verifica si la solicitud fue exitosa
            if (response.IsSuccessStatusCode)            
                context.Logger.Log("El archivo se envió exitosamente.");            
            else
            {
                context.Logger.Log("Error al enviar el archivo a gravty en UploadToGravtyAsync.  StatusCode: " + response.StatusCode.ToString());
                context.Logger.Log("Error al conseguir la URL firmada en UploadToGravtyAsync.  Response: " + JsonConvert.DeserializeObject<string>(contentResponse));               
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError("Error en DownloadS3FileAsync: " + ex.Message);
            throw;
        }
    }
 }
