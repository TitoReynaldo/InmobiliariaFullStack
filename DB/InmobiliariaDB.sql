DROP DATABASE IF EXISTS InmobiliariaDB;
CREATE DATABASE InmobiliariaDB CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE InmobiliariaDB;

CREATE TABLE Usuarios (
    UsuarioID INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Rol ENUM('Admin', 'Asesor', 'Cliente') DEFAULT 'Cliente',
    FechaRegistro DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Clientes (
    ClienteID INT AUTO_INCREMENT PRIMARY KEY,
    UsuarioID INT,
    DNI VARCHAR(8) NOT NULL UNIQUE,
    Nombres VARCHAR(100) NOT NULL,
    Apellidos VARCHAR(100) NOT NULL,
    FechaNacimiento DATE NOT NULL,
    SueldoMensual DECIMAL(30, 15) NOT NULL,
    IngresoNeto DECIMAL(30, 15) NULL,
    SituacionLaboral VARCHAR(100) NULL,
    ScoreEndeudamiento INT NULL,
    Email VARCHAR(100),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UsuarioID) REFERENCES Usuarios(UsuarioID) ON DELETE CASCADE
);

CREATE TABLE Propiedades (
    PropiedadID INT AUTO_INCREMENT PRIMARY KEY,
    Direccion VARCHAR(255) NOT NULL,
    PrecioVenta DECIMAL(30, 15) NOT NULL,
    Estado ENUM('Disponible', 'Vendido') DEFAULT 'Disponible',
    EsBonoVerde BOOLEAN DEFAULT FALSE,
    IdProyecto INT NULL,
    UbicacionGeografica VARCHAR(255) NULL,
    TipoInmueble VARCHAR(100) NULL,
    AreaTotal DECIMAL(30, 15) NULL,
    TasacionActivo DECIMAL(30, 15) NULL,
    FlagBonoVerde BOOLEAN DEFAULT FALSE,
    FlagBFH BOOLEAN DEFAULT FALSE,
    MonedaBase ENUM('PEN', 'USD') DEFAULT 'PEN'
);

CREATE TABLE ConfigFinanciera (
    ConfigID INT AUTO_INCREMENT PRIMARY KEY,
    Moneda ENUM('PEN', 'USD') DEFAULT 'PEN',
    CuotaInicial DECIMAL(30, 15) NOT NULL,
    PlazoMeses INT NOT NULL,
    TipoTasa VARCHAR(50) DEFAULT 'Efectiva',
    TasaInteres DECIMAL(30, 15) NOT NULL,
    DiasPorPeriodo INT DEFAULT 30,
    DiasPorAnio INT DEFAULT 360,
    TipoGracia ENUM('Sin Gracia', 'Parcial', 'Total') DEFAULT 'Sin Gracia',
    MesesGracia INT DEFAULT 0,
    CostesNotariales DECIMAL(30, 15) DEFAULT 0,
    CostesRegistrales DECIMAL(30, 15) DEFAULT 0,
    Tasacion DECIMAL(30, 15) DEFAULT 0,
    Portes DECIMAL(30, 15) DEFAULT 0,
    GastosAdministracion DECIMAL(30, 15) DEFAULT 0,
    PorcentajeDesgravamen DECIMAL(30, 15) NOT NULL,
    PorcentajeSeguroInmueble DECIMAL(30, 15) NOT NULL,
    TasaDescuento DECIMAL(30, 15) DEFAULT 12.5,
    PrepagosJson TEXT NULL
);

CREATE TABLE MaestroBonos (
    BonoID INT AUTO_INCREMENT PRIMARY KEY,
    TipoBono VARCHAR(50) NOT NULL,
    ValorViviendaMin DECIMAL(30, 15) NOT NULL,
    ValorViviendaMax DECIMAL(30, 15) NOT NULL,
    ValorBono DECIMAL(30, 15) NOT NULL
);

CREATE TABLE Simulaciones (
    SimulacionID INT AUTO_INCREMENT PRIMARY KEY,
    ClienteID INT NOT NULL,
    PropiedadID INT NOT NULL,
    ConfigID INT NOT NULL,
    MontoPrestamo DECIMAL(30, 15) NOT NULL,
    TasaEfectivaAnual DECIMAL(30, 15) NOT NULL,
    VAN DECIMAL(30, 15) NULL,
    TIR DECIMAL(30, 15) NULL,
    TCEA DECIMAL(30, 15) NULL,
    FechaSimulacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ClienteID) REFERENCES Clientes(ClienteID) ON DELETE CASCADE,
    FOREIGN KEY (PropiedadID) REFERENCES Propiedades(PropiedadID) ON DELETE CASCADE,
    FOREIGN KEY (ConfigID) REFERENCES ConfigFinanciera(ConfigID) ON DELETE CASCADE
);

CREATE TABLE DetalleCronograma (
    DetalleID INT AUTO_INCREMENT PRIMARY KEY,
    SimulacionID INT NOT NULL,
    NroCuota INT NOT NULL,
    TasaEfectivaPeriodo DECIMAL(30, 15) NOT NULL,
    SaldoInicial DECIMAL(30, 15) NOT NULL,
    Interes DECIMAL(30, 15) NOT NULL,
    Amortizacion DECIMAL(30, 15) NOT NULL,
    Cuota DECIMAL(30, 15) NOT NULL,
    SeguroDesgravamen DECIMAL(30, 15) NOT NULL,
    SeguroInmueble DECIMAL(30, 15) NOT NULL,
    CuotaTotal DECIMAL(30, 15) NOT NULL,
    SaldoFinal DECIMAL(30, 15) NOT NULL,
    FOREIGN KEY (SimulacionID) REFERENCES Simulaciones(SimulacionID) ON DELETE CASCADE
);

INSERT INTO Usuarios (Username, PasswordHash, Rol) VALUES ('admin', 'admin123', 'Admin');

INSERT INTO MaestroBonos (TipoBono, ValorViviendaMin, ValorViviendaMax, ValorBono) VALUES
('Bono Buen Pagador', 68800.00, 98100.00, 27400.00),
('Bono Buen Pagador', 98100.01, 146900.00, 22800.00),
('Bono Buen Pagador', 146900.01, 244600.00, 20900.00),
('Bono Buen Pagador', 244600.01, 362100.00, 7800.00),
('Bono Verde', 0.00, 362100.00, 5500.00);