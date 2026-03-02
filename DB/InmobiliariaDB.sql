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
    NombreBanco VARCHAR(50) NOT NULL,
    TipoTasa ENUM('Nominal', 'Efectiva') DEFAULT 'Efectiva',
    TasaInteres DECIMAL(30, 15) NOT NULL,
    PorcentajeDesgravamen DECIMAL(30, 15) NOT NULL,
    PorcentajeSeguroInmueble DECIMAL(30, 15) NOT NULL,
    PlazoMinimoMeses INT NOT NULL,
    PlazoMaximoMeses INT NOT NULL
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
    CuotaInicial DECIMAL(30, 15) NOT NULL,
    PlazoMeses INT NOT NULL,
    TasaEfectivaAnual DECIMAL(30, 15) NOT NULL,
    FechaSimulacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    VAN DECIMAL(30, 15) NULL,
    TIR DECIMAL(30, 15) NULL,
    TCEA DECIMAL(30, 15) NULL,
    TipoGracia ENUM('Sin Gracia', 'Parcial', 'Total') DEFAULT 'Sin Gracia',
    MesesGracia INT DEFAULT 0,
    Moneda ENUM('PEN', 'USD') DEFAULT 'PEN',
    TipoAmortizacion VARCHAR(50) DEFAULT 'Francesa',
    TasaSeguroDesgravamen DECIMAL(30, 15) NULL,
    TasaSeguroInmueble DECIMAL(30, 15) NULL,
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
INSERT INTO ConfigFinanciera (NombreBanco, TipoTasa, TasaInteres, PorcentajeDesgravamen, PorcentajeSeguroInmueble, PlazoMinimoMeses, PlazoMaximoMeses) 
VALUES ('Banco Continental', 'Efectiva', 0.125000000000000, 0.000500000000000, 0.000200000000000, 60, 300);
INSERT INTO MaestroBonos (TipoBono, ValorViviendaMin, ValorViviendaMax, ValorBono) 
VALUES ('Bono Buen Pagador', 65000.00, 120000.00, 24000.00), ('Bono Verde', 0, 343900.00, 5000.00);