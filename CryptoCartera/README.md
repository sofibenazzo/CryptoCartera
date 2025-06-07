# CryptoCartera API

API REST para la gestión de transacciones de criptomonedas, donde los usuarios pueden registrar 
compras y ventas de criptomonedas. El sistema consulta precios en tiempo real a través de la API 
pública de CriptoYa.

## Tecnologías usadas

- .NET 6 (o superior) con ASP.NET Core Web API  
- Entity Framework Core para acceso a base de datos SQL  
- HttpClient para consumo de API externa  
- JSON para intercambio de datos

## Requisitos previos

- Base de datos SQL Server (o compatible) configurada  
- Herramienta para probar API (Postman, Insomnia, curl, etc.)

## Configurar la cadena de conexión a la base de datos en appsettings.json

"ConnectionStrings": {
  "DefaultConnection": ""
}

## Ejecutar migraciones para crear las tablas

dotnet ef database update

## Ejecutar la API

dotnet run

