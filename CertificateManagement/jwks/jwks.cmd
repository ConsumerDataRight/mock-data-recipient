openssl req -new -newkey rsa:2048 -keyout jwks.key -sha256 -nodes -out jwks.csr -config jwks.cnf
openssl req -in jwks.csr -noout -text
openssl x509 -req -days 1826 -in jwks.csr -out jwks.pem -extfile jwks.ext -signkey jwks.key
openssl pkcs12 -inkey jwks.key -in jwks.pem -export -out jwks.pfx
openssl pkcs12 -in jwks.pfx -noout -info
