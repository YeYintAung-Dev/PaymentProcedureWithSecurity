
# Payment Transaction with Secure Stored Procedure & .NET Console App

## Overview

This project demonstrates how to implement a **secure payment transaction** using:

- SQL Server **stored procedure** with transaction management and security  
- A dedicated SQL login (`PaymentAppLogin`) for executing payment logic  
- A .NET 8 **console application** using EF Core to call the stored procedure safely  
- Basic validation, error handling, and audit logging in the database  

---

## Setup Steps

### 1. Create SQL Server Login and User

- Create a dedicated login for the app:

```sql
CREATE LOGIN PaymentAppLogin WITH PASSWORD = 'StrongP@ssw0rd!';
USE PaymentDB;
CREATE USER PaymentAppUser FOR LOGIN PaymentAppLogin;
```

- Grant this user **only EXECUTE permission** on the stored procedure.

---

### 2. Create Tables

- `Tbl_Accounts`: Stores account info and balances  
- `Tbl_Transactions`: Records payment transactions  
- `Tbl_PaymentAudit`: Logs all payment attempts and statuses  

Sample table creation scripts are included in the project.

---

### 3. Create the Stored Procedure (`usp_ProcessPayment`)

- Accepts parameters: `@FromAccountId`, `@ToAccountId`, `@Amount`  
- Validates caller login using `ORIGINAL_LOGIN()` to allow only `PaymentAppLogin`  
- Validates parameters (amount > 0, accounts exist)  
- Uses transactions and TRY/CATCH to ensure atomicity  
- Inserts audit logs for start, success, failure, unauthorized attempts  

---

### 4. Insert Sample Data

Insert test accounts for demo purposes:

```sql
INSERT INTO Tbl_Accounts (AccountName, Balance) VALUES 
('Alice', 1000.00),
('Bob', 500.00),
('Charlie', 200.00);
```

---

### 5. Configure .NET Console App

- Use EF Core with connection string:

```
Server=YOURSERVER;Database=PaymentDB;User Id=PaymentAppLogin;Password=StrongP@ssw0rd!;TrustServerCertificate=True;
```

- Call the stored procedure securely with:

```csharp
int affected = await ctx.Database.ExecuteSqlInterpolatedAsync(
    $"EXEC usp_ProcessPayment @FromAccountId={from}, @ToAccountId={to}, @Amount={amt}");
```

- Handle exceptions to catch validation or unauthorized errors.

---

### 6. Testing and Usage

- Connect to SQL Server using `PaymentAppLogin` in SSMS to manually test the procedure  
- Run the console app, enter account IDs and amount, and observe the payment processing  
- Check `Tbl_PaymentAudit` and `Tbl_Transactions` for logs and history  

---

## Notes on Security and Scalability

- Using a dedicated SQL login with stored procedure restrictions enhances security  
- `ORIGINAL_LOGIN()` enforces that only the app’s login can execute sensitive logic  
- Multiple users can simultaneously use the app as the same SQL login without conflict  
- Application layer should handle user authentication and pass valid requests  

---

## Useful Links

- [ORIGINAL_LOGIN() function](https://learn.microsoft.com/en-us/sql/t-sql/functions/original-login-transact-sql)  
- [EF Core stored procedure execution](https://learn.microsoft.com/en-us/ef/core/querying/raw-sql)  
- [SQL Server stored procedure error handling](https://learn.microsoft.com/en-us/sql/relational-databases/stored-procedures/stored-procedures-database-engine)  

---
## Stored Procedure
USE [PaymentDB]
GO
/****** Object:  StoredProcedure [dbo].[usp_ProcessPayment]    Script Date: 8/4/2025 10:24:23 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[usp_ProcessPayment]
    @FromAccountId INT,
    @ToAccountId INT,
    @Amount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @caller SYSNAME = ORIGINAL_LOGIN();

    -- Log start
    INSERT INTO Tbl_PaymentAudit (FromAccountId, ToAccountId, Amount, AttemptedBy, Status)
    VALUES (@FromAccountId, @ToAccountId, @Amount, @caller, 'Started');

    -- Check caller security
    IF @caller <> 'PaymentAppLogin'
    BEGIN
        INSERT INTO Tbl_PaymentAudit (FromAccountId, ToAccountId, Amount, AttemptedBy, Status)
        VALUES (@FromAccountId, @ToAccountId, @Amount, @caller, 'Unauthorized');

        RAISERROR('Unauthorized Access', 16, 1);
        RETURN;
    END

    -- Validate parameters
    IF @Amount <= 0 
       OR NOT EXISTS(SELECT 1 FROM Tbl_Accounts WHERE AccountId = @FromAccountId)
       OR NOT EXISTS(SELECT 1 FROM Tbl_Accounts WHERE AccountId = @ToAccountId)
    BEGIN
        INSERT INTO Tbl_PaymentAudit (FromAccountId, ToAccountId, Amount, AttemptedBy, Status)
        VALUES (@FromAccountId, @ToAccountId, @Amount, @caller, 'ValidationFailed');

        RAISERROR('Invalid Parameters', 16, 1);
        RETURN;
    END

    -- Begin transaction
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Deduct from sender
        UPDATE Tbl_Accounts 
        SET Balance = Balance - @Amount 
        WHERE AccountId = @FromAccountId;

        -- Credit to receiver
        UPDATE Tbl_Accounts 
        SET Balance = Balance + @Amount 
        WHERE AccountId = @ToAccountId;

        -- Log transaction
        INSERT INTO Tbl_Transactions (FromAccountId, ToAccountId, Amount)
        VALUES (@FromAccountId, @ToAccountId, @Amount);

        COMMIT TRANSACTION;

        -- Mark success
        INSERT INTO Tbl_PaymentAudit (FromAccountId, ToAccountId, Amount, AttemptedBy, Status)
        VALUES (@FromAccountId, @ToAccountId, @Amount, @caller, 'Success');
    END TRY
    BEGIN CATCH
        -- Rollback on error
        ROLLBACK TRANSACTION;

        -- Log error
        INSERT INTO Tbl_PaymentAudit (FromAccountId, ToAccountId, Amount, AttemptedBy, Status)
        VALUES (@FromAccountId, @ToAccountId, @Amount, @caller, 'Error');

        -- Return error
        DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@msg, 16, 1);
    END CATCH;
END;


If you want me to prepare the full `README.md` file text or any additional docs, just ask!
