-- Создание базы данных
CREATE DATABASE AccessControlDB;
GO

USE AccessControlDB;
GO

-- Таблица ролей
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

-- Таблица отделов
CREATE TABLE Departments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DepartmentName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL
);

-- Таблица зон доступа
CREATE TABLE AccessZones (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ZoneName NVARCHAR(100) NOT NULL,
    RequiredClearance INT NOT NULL DEFAULT 1,
    Description NVARCHAR(255) NULL
);

-- Таблица типов пропусков
CREATE TABLE PassTypes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TypeName NVARCHAR(50) NOT NULL,
    DefaultValidityDays INT NOT NULL DEFAULT 365
);

-- Таблица сотрудников
CREATE TABLE Employees (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    DepartmentId INT NULL,
    Position NVARCHAR(100) NULL,
    SecurityClearance INT NULL DEFAULT 1,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    IsActive BIT NULL DEFAULT 1,
    PhotoPath NVARCHAR(255) NULL,
    CreatedAt DATETIME NULL DEFAULT GETDATE(),
    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);

-- Таблица пользователей системы (связь с сотрудниками)
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    RoleId INT NOT NULL,
    EmployeeId INT NULL,  -- Связь с сотрудником
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)  -- Внешний ключ к Employees
);

-- Таблица пропусков
CREATE TABLE Passes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CardNumber NVARCHAR(50) NOT NULL UNIQUE,
    EmployeeId INT NOT NULL,
    AccessZoneId INT NOT NULL,
    PassTypeId INT NOT NULL,
    StartDate DATE NULL,
    EndDate DATE NULL,
    Status NVARCHAR(20) NOT NULL CHECK (Status IN (N'Активен', N'Заблокирован', N'Просрочен')),
    Notes NVARCHAR(255) NULL,
    CreatedAt DATETIME NULL DEFAULT GETDATE(),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    FOREIGN KEY (AccessZoneId) REFERENCES AccessZones(Id),
    FOREIGN KEY (PassTypeId) REFERENCES PassTypes(Id)
);

-- Таблица журнала проходов
CREATE TABLE AccessLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PassId INT NULL,
    EmployeeId INT NULL,
    Timestamp DATETIME NULL DEFAULT GETDATE(),
    ZoneId INT NOT NULL,
    Result NVARCHAR(20) NOT NULL CHECK (Result IN (N'Доступ разрешен', N'Доступ запрещен')),
    DenialReason NVARCHAR(255) NULL,
    RegisteredBy NVARCHAR(100) NULL,
    FOREIGN KEY (PassId) REFERENCES Passes(Id),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    FOREIGN KEY (ZoneId) REFERENCES AccessZones(Id)
);

-- Вставка начальных данных
INSERT INTO Roles (RoleName) VALUES (N'Администратор'), (N'Сотрудник охраны');

INSERT INTO Departments (DepartmentName, Description) VALUES 
(N'Администрация', N'Руководство предприятия'),
(N'Производство', N'Производственный цех'),
(N'Склад', N'Складское хозяйство'),
(N'Охрана', N'Служба безопасности');

INSERT INTO AccessZones (ZoneName, RequiredClearance, Description) VALUES 
(N'Входная зона', 1, N'Главный вход на территорию'),
(N'Производственный цех', 2, N'Производственные помещения'),
(N'Склад', 3, N'Складские помещения'),
(N'Административное здание', 2, N'Офисные помещения');

INSERT INTO PassTypes (TypeName, DefaultValidityDays) VALUES 
(N'Постоянный', 1095),
(N'Временный', 30),
(N'Разовый', 1);

-- Вставка сотрудников
INSERT INTO Employees (FullName, DepartmentId, Position, SecurityClearance, Phone, IsActive) VALUES 
(N'Иванов Иван Иванович', 1, N'Директор', 3, '+79111234567', 1),
(N'Петров Петр Петрович', 2, N'Начальник цеха', 2, '+79117654321', 1),
(N'Сидорова Анна Сергеевна', 3, N'Кладовщик', 2, '+79119876543', 1),
(N'Кузнецов Алексей Владимирович', 2, N'Оператор станка', 1, '+79115556677', 1),
(N'Смирнова Екатерина Дмитриевна', 1, N'Секретарь', 1, '+79113334455', 1),
(N'Васильев Олег Николаевич', 4, N'Начальник охраны', 2, '+79112223344', 1);

-- Создание пользователей (связь с сотрудниками)
-- Пароль: 123456, хэш SHA256: 8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92
INSERT INTO Users (Username, PasswordHash, FullName, RoleId, EmployeeId, IsActive) VALUES 
('admin', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', N'Администратор системы', 1, 1, 1),
('security', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', N'Сотрудник охраны', 2, 6, 1);

-- Вставка пропусков
INSERT INTO Passes (CardNumber, EmployeeId, AccessZoneId, PassTypeId, StartDate, EndDate, Status) VALUES 
('1001', 1, 4, 1, '2024-01-01', '2027-01-01', N'Активен'),
('1002', 2, 2, 1, '2024-01-01', '2027-01-01', N'Активен'),
('1003', 3, 3, 1, '2024-01-01', '2027-01-01', N'Активен'),
('1004', 4, 2, 1, '2024-01-01', '2027-01-01', N'Активен'),
('1005', 5, 4, 1, '2024-01-01', '2027-01-01', N'Активен'),
('1006', 6, 1, 1, '2024-01-01', '2027-01-01', N'Активен');

-- Вставка тестовых записей в журнал
INSERT INTO AccessLogs (PassId, EmployeeId, ZoneId, Result, RegisteredBy) VALUES 
(1, 1, 4, N'Доступ разрешен', N'Система'),
(2, 2, 2, N'Доступ разрешен', N'Система'),
(3, 3, 3, N'Доступ разрешен', N'Система'),
(4, 4, 2, N'Доступ разрешен', N'Система'),
(5, 5, 4, N'Доступ разрешен', N'Система'),
(6, 6, 1, N'Доступ разрешен', N'Система');