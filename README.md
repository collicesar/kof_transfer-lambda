# kof_transfer-lambda

Lambda que se logea y obtiene la URL firmada para subir archivos al S3 de Gravty de KOF

Nota:

Necesita tener configurado las siguiente variables de entorno

1. ApiKey     
2. GravtyLoginURL (Contiene la URL para hacer la petición de logeo)
3. UserName (Contiene el nombre del usuario a logearse)
4. Password  (Contiene la contraseña del usuario a logearse )
5. GravtyGetSignedURL (Contiene la URL para obtener la URL firmada de Gravty-KOF)
6. BatchId  (Contiene el identificador del Batch)
7. SponsorId (Contiene el identificador del Sponsor)
