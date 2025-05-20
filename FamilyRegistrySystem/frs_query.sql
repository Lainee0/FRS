CREATE TABLE Barangays (
    BarangayID INT PRIMARY KEY IDENTITY(1,1),
    BarangayName NVARCHAR(100) NOT NULL
);

CREATE TABLE Households (
    HouseholdNumber INT PRIMARY KEY,
    BarangayID INT FOREIGN KEY REFERENCES Barangays(BarangayID),
    DateRegistered DATETIME DEFAULT GETDATE()
);

CREATE TABLE FamilyMembers (
    MemberID INT PRIMARY KEY IDENTITY(1,1),
    HouseholdNumber INT FOREIGN KEY REFERENCES Households(HouseholdNumber),
    IsHead BIT DEFAULT 0,
    RowIndicator NVARCHAR(50),
    LastName NVARCHAR(100) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    MiddleName NVARCHAR(100),
    Relationship NVARCHAR(100),
    Birthday DATE,
    Age INT,
    Sex NVARCHAR(10),
    CivilStatus NVARCHAR(50)
);