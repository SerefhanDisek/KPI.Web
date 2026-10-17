USE [kpi_evaluation_db];

IF OBJECT_ID(N'profession', N'U') IS NULL
BEGIN
    CREATE TABLE profession (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_profession] PRIMARY KEY,
        [Name] nvarchar(100) NOT NULL CONSTRAINT [UQ_profession_name] UNIQUE,
        [IsActive] bit NOT NULL CONSTRAINT [DF_profession_active] DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_profession_created] DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID(N'employee', N'U') IS NULL
BEGIN
    CREATE TABLE employee (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_employee] PRIMARY KEY,
        [FullName] nvarchar(200) NOT NULL,
        [ProfessionId] int NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_employee_active] DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_employee_created] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [FK_employee_profession] FOREIGN KEY ([ProfessionId]) REFERENCES profession([Id])
    );
    CREATE INDEX [IX_employee_full_name] ON employee([FullName]);
END;

IF OBJECT_ID(N'kpi_criterion', N'U') IS NULL
BEGIN
    CREATE TABLE kpi_criterion (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_kpi_criterion] PRIMARY KEY,
        [ProfessionId] int NULL,
        [Title] nvarchar(1000) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [IsCommon] bit NOT NULL CONSTRAINT [DF_kpi_common] DEFAULT 0,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_kpi_active] DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_kpi_created] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [FK_kpi_profession] FOREIGN KEY ([ProfessionId]) REFERENCES profession([Id]),
        CONSTRAINT [CK_kpi_scope] CHECK (([IsCommon]=1 AND [ProfessionId] IS NULL) OR ([IsCommon]=0 AND [ProfessionId] IS NOT NULL)),
        CONSTRAINT [CK_kpi_order] CHECK ([DisplayOrder] > 0)
    );
    CREATE INDEX [IX_kpi_filter] ON kpi_criterion([ProfessionId],[IsCommon],[IsActive]);
END;

IF OBJECT_ID(N'evaluation_period', N'U') IS NULL
BEGIN
    CREATE TABLE evaluation_period (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_evaluation_period] PRIMARY KEY,
        [Name] nvarchar(100) NOT NULL CONSTRAINT [UQ_period_name] UNIQUE,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_period_active] DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_period_created] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [CK_period_dates] CHECK ([EndDate] >= [StartDate])
    );
END;

IF OBJECT_ID(N'employee_evaluation', N'U') IS NULL
BEGIN
    CREATE TABLE employee_evaluation (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_employee_evaluation] PRIMARY KEY,
        [EmployeeId] int NOT NULL,
        [EvaluationPeriodId] int NOT NULL,
        [EmployeeNameSnapshot] nvarchar(200) NOT NULL,
        [ProfessionIdSnapshot] int NOT NULL,
        [ProfessionNameSnapshot] nvarchar(100) NOT NULL,
        [PeriodNameSnapshot] nvarchar(100) NOT NULL,
        [EvaluationDate] datetime2 NOT NULL CONSTRAINT [DF_evaluation_date] DEFAULT SYSUTCDATETIME(),
        [TotalScore] int NOT NULL,
        [AverageScore] decimal(4,2) NOT NULL,
        [GeneralNote] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_evaluation_created] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [FK_evaluation_employee] FOREIGN KEY ([EmployeeId]) REFERENCES employee([Id]),
        CONSTRAINT [FK_evaluation_period] FOREIGN KEY ([EvaluationPeriodId]) REFERENCES evaluation_period([Id]),
        CONSTRAINT [UQ_employee_period] UNIQUE ([EmployeeId],[EvaluationPeriodId]),
        CONSTRAINT [CK_total_score] CHECK ([TotalScore] > 0),
        CONSTRAINT [CK_average_score] CHECK ([AverageScore] BETWEEN 1 AND 5)
    );
    CREATE INDEX [IX_evaluation_profession_snapshot] ON employee_evaluation([ProfessionIdSnapshot]);
END;

IF OBJECT_ID(N'employee_evaluation_detail', N'U') IS NULL
BEGIN
    CREATE TABLE employee_evaluation_detail (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_evaluation_detail] PRIMARY KEY,
        [EmployeeEvaluationId] bigint NOT NULL,
        [KpiCriterionId] int NOT NULL,
        [KpiTitleSnapshot] nvarchar(1000) NOT NULL,
        [IsCommonSnapshot] bit NOT NULL,
        [DisplayOrderSnapshot] int NOT NULL,
        [Score] int NOT NULL,
        [Note] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_detail_created] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_detail_evaluation] FOREIGN KEY ([EmployeeEvaluationId]) REFERENCES employee_evaluation([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_detail_kpi] FOREIGN KEY ([KpiCriterionId]) REFERENCES kpi_criterion([Id]),
        CONSTRAINT [UQ_evaluation_criterion] UNIQUE ([EmployeeEvaluationId],[KpiCriterionId]),
        CONSTRAINT [CK_score] CHECK ([Score] BETWEEN 1 AND 5)
    );
END;
