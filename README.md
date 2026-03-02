# 🏦 Inmobiliaria API & Simulador Hipotecario

Sistema Fullstack de simulación financiera hipotecaria basado en el Método de Amortización Francés. Permite proyectar cronogramas de pago exactos, gestionar subsidios estatales y mantener un historial de auditoría inmutable.

##  Stack Tecnológico

* **Frontend:** Vue.js 3, Vite, Pinia (Gestor de Estado), CSS3 (CSS Variables).
* **Backend:** C# .NET 10, ASP.NET Core Web API, Entity Framework Core.
* **Base de Datos:** MySQL 8.0+ (Estructura Relacional Normalizada de Alta Precisión DECIMAL 30,15).

##  Características Core

* **Motor Matemático:** Cálculo preciso de Tasa de Coste Efectivo Anual (TCEA), Valor Actual Neto (VAN) y Tasa Interna de Retorno (TIR).
* **Sistema Multidivisa:** Motor reactivo para cálculos y persistencia bidireccional en Soles (PEN) y Dólares (USD).
* **Estructura de Cuotas:** Integración algorítmica de Seguro de Desgravamen, Seguro de Riesgo, Gastos Administrativos y Portes.
* **Periodos de Gracia:** Soporte paramétrico para configuración de Gracia Total o Gracia Parcial.
* **Gestión de Subsidios:** Evaluación y aplicación automática de Bono Mivivienda Sostenible (Verde) y Bono del Buen Pagador (BBP) basados en rangos de tasación.
* **Auditoría Criptográfica:** Trazabilidad de cada simulación vinculada a la sesión del usuario.
* **Amortizaciones Extraordinarias y Pagos Anticipados:** El sistema permite la inyección de capital extraordinario en cualquier periodo del cronograma, reduciendo el saldo deudor de forma inmediata.
  * **Reducción de Cuota:** El motor ejecuta un recálculo dinámico de la anualidad (R) manteniendo el plazo original, lo que disminuye la carga financiera mensual del cliente.
  * **Reducción de Plazo:** El algoritmo mantiene la cuota constante pero liquida el capital más rápido, recalculando la fecha de término del crédito y generando un ahorro significativo en intereses totales.
* **Cierre Automático:** Validación lógica que detecta si el pago extraordinario cubre la totalidad de la deuda, forzando un cierre en cero exacto y finalizando el cronograma de forma automática.

##  Guía de Despliegue Local

### 1. Motor de Base de Datos (MySQL)
1. Localizar el script de aprovisionamiento en la carpeta raíz: `InmobiliariaDB/InmobiliariaDB.sql`.
2. Ejecutar el script en su servidor MySQL local para generar el esquema relacional limpio e inyectar los datos semilla (Configuración Bancaria y Leyes de Subsidio).

### 2. Capa Backend (.NET 10)

El núcleo del sistema expone la lógica de negocio mediante una API REST protegida por tokens e interactúa con la base de datos mediante Entity Framework Core.

**Dependencias Core (NuGet):**
* `Pomelo.EntityFrameworkCore.MySql` (v9.0.0): Abstracción ORM nativa y de alto rendimiento.
* `Microsoft.AspNetCore.Authentication.JwtBearer` & `System.IdentityModel.Tokens.Jwt`: Infraestructura de seguridad, autorización y validación de sesiones.
* `Microsoft.AspNetCore.OpenApi`: Especificación estandarizada de endpoints.

**Instrucciones de Despliegue:**
1. Navegar al directorio del núcleo (ajustar ruta según clonación): `cd Inmobiliaria.API`
2. Inyectar credenciales de base de datos modificando el nodo `DefaultConnection` dentro del archivo `appsettings.json`.
3. Restaurar dependencias y desplegar el servidor Kestrel:
```bash
dotnet restore
dotnet run
```
### 3. Capa Frontend (Vue 3)

Aplicación de Página Única (SPA) optimizada con Vite, diseñada para la reactividad de cálculos financieros, visualización gráfica y exportación documental.

**Dependencias Core (NPM):**

* `vue` & `vue-router`: Núcleo reactivo y enrutamiento estructural.

* `pinia`: Gestión de estado global (Store) para la persistencia de los simuladores.

* `axios`: Cliente HTTP asíncrono para el consumo de la API REST.

* `chart.js` & `vue-chartjs`: Motor de renderizado dinámico para dashboards y gráficos financieros.

**Instrucciones de Despliegue:**

1. Navegar al directorio del cliente: `cd Inmobiliaria-frontend`
2. Instalar el árbol de dependencias e inicializar el servidor Vite:

```bash
npm install
npm run dev
```