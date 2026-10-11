IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId] bigint NOT NULL IDENTITY,
        [Timestamp] datetime2 NOT NULL,
        [UserId] nvarchar(450) NULL,
        [UserName] nvarchar(150) NOT NULL,
        [Action] nvarchar(30) NOT NULL,
        [EntityName] nvarchar(80) NOT NULL,
        [EntityId] nvarchar(60) NULL,
        [Details] nvarchar(2000) NULL,
        [IpAddress] nvarchar(60) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [ContactMessages] (
        [ContactMessageId] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Message] nvarchar(3000) NOT NULL,
        [IsRead] bit NOT NULL,
        [IsResolved] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([ContactMessageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Announcements] (
        [AnnouncementId] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [Audience] int NOT NULL,
        [IsPinned] bit NOT NULL,
        [PublishedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [CreatedById] nvarchar(450) NULL,
        CONSTRAINT [PK_Announcements] PRIMARY KEY ([AnnouncementId]),
        CONSTRAINT [FK_Announcements_AspNetUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Competitions] (
        [CompetitionId] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Theme] nvarchar(100) NULL,
        [Description] nvarchar(2000) NULL,
        [Conditions] nvarchar(3000) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [CreatedById] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Competitions] PRIMARY KEY ([CompetitionId]),
        CONSTRAINT [FK_Competitions_AspNetUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Exhibitions] (
        [ExhibitionId] int NOT NULL IDENTITY,
        [ExhibitionName] nvarchar(300) NOT NULL,
        [Description] nvarchar(3000) NULL,
        [Venue] nvarchar(300) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [Country] nvarchar(100) NOT NULL,
        [ExhibitionStartDate] datetime2 NOT NULL,
        [ExhibitionEndDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [CommissionPercent] decimal(18,2) NOT NULL,
        [AdditionalNotes] nvarchar(1000) NULL,
        [CreatedById] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Exhibitions] PRIMARY KEY ([ExhibitionId]),
        CONSTRAINT [FK_Exhibitions_AspNetUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Notifications] (
        [NotificationId] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Message] nvarchar(500) NULL,
        [Url] nvarchar(300) NULL,
        [Type] int NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Staff] (
        [StaffId] int NOT NULL IDENTITY,
        [ApplicationUserId] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Gender] nvarchar(20) NOT NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [EmergencyContact] nvarchar(100) NULL,
        [Qualification] nvarchar(200) NULL,
        [DateJoined] datetime2 NOT NULL,
        [HandlesClass] nvarchar(300) NULL,
        [HandlesSubject] nvarchar(300) NULL,
        [Designation] nvarchar(100) NULL,
        [Remarks] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Staff] PRIMARY KEY ([StaffId]),
        CONSTRAINT [FK_Staff_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Students] (
        [StudentId] int NOT NULL IDENTITY,
        [ApplicationUserId] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Gender] nvarchar(20) NOT NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [ParentOrGuardianName] nvarchar(100) NULL,
        [ParentOrGuardianPhone] nvarchar(20) NULL,
        [EnrollmentNumber] nvarchar(50) NOT NULL,
        [AdmissionDate] datetime2 NOT NULL,
        [Course] nvarchar(150) NOT NULL,
        [ClassName] nvarchar(100) NULL,
        [Remarks] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([StudentId]),
        CONSTRAINT [FK_Students_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [CompetitionAwards] (
        [CompetitionAwardId] int NOT NULL IDENTITY,
        [CompetitionId] int NOT NULL,
        [AwardTitle] nvarchar(100) NOT NULL,
        [AwardDescription] nvarchar(500) NULL,
        [PrizeMoney] decimal(18,2) NULL,
        [Rank] int NOT NULL,
        CONSTRAINT [PK_CompetitionAwards] PRIMARY KEY ([CompetitionAwardId]),
        CONSTRAINT [FK_CompetitionAwards_Competitions_CompetitionId] FOREIGN KEY ([CompetitionId]) REFERENCES [Competitions] ([CompetitionId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Submissions] (
        [SubmissionId] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [CompetitionId] int NOT NULL,
        [PaintingTitle] nvarchar(200) NOT NULL,
        [ImageFilePath] nvarchar(500) NOT NULL,
        [ThumbnailPath] nvarchar(500) NULL,
        [OriginalFileName] nvarchar(255) NULL,
        [FileSizeInBytes] bigint NOT NULL,
        [ImageWidth] int NOT NULL,
        [ImageHeight] int NOT NULL,
        [AccompanyingText] nvarchar(max) NOT NULL,
        [TextType] nvarchar(50) NOT NULL,
        [CreativeDescription] nvarchar(3000) NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [LastUpdatedAt] datetime2 NULL,
        [IsLateSubmission] bit NOT NULL,
        [Grade] int NOT NULL,
        CONSTRAINT [PK_Submissions] PRIMARY KEY ([SubmissionId]),
        CONSTRAINT [FK_Submissions_Competitions_CompetitionId] FOREIGN KEY ([CompetitionId]) REFERENCES [Competitions] ([CompetitionId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Submissions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Awards] (
        [AwardId] int NOT NULL IDENTITY,
        [CertificateNumber] nvarchar(30) NOT NULL,
        [StudentId] int NOT NULL,
        [CompetitionId] int NOT NULL,
        [CompetitionAwardId] int NULL,
        [SubmissionId] int NOT NULL,
        [IssuedById] nvarchar(450) NULL,
        [CustomAwardTitle] nvarchar(200) NULL,
        [Remarks] nvarchar(2000) NULL,
        [DateIssued] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Awards] PRIMARY KEY ([AwardId]),
        CONSTRAINT [FK_Awards_AspNetUsers_IssuedById] FOREIGN KEY ([IssuedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Awards_CompetitionAwards_CompetitionAwardId] FOREIGN KEY ([CompetitionAwardId]) REFERENCES [CompetitionAwards] ([CompetitionAwardId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Awards_Competitions_CompetitionId] FOREIGN KEY ([CompetitionId]) REFERENCES [Competitions] ([CompetitionId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Awards_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Awards_Submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [Submissions] ([SubmissionId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [Evaluations] (
        [EvaluationId] int NOT NULL IDENTITY,
        [SubmissionId] int NOT NULL,
        [EvaluatedById] nvarchar(450) NULL,
        [Grade] int NOT NULL,
        [PositivePoints] nvarchar(2000) NULL,
        [NegativePoints] nvarchar(2000) NULL,
        [AreasOfImprovement] nvarchar(2000) NULL,
        [GeneralRemarks] nvarchar(2000) NULL,
        [EvaluatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Evaluations] PRIMARY KEY ([EvaluationId]),
        CONSTRAINT [FK_Evaluations_AspNetUsers_EvaluatedById] FOREIGN KEY ([EvaluatedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Evaluations_Submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [Submissions] ([SubmissionId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [ExhibitionListings] (
        [ExhibitionListingId] int NOT NULL IDENTITY,
        [ExhibitionId] int NOT NULL,
        [SubmissionId] int NOT NULL,
        [ForwardedById] nvarchar(450) NULL,
        [StudentQuotedPrice] decimal(18,2) NOT NULL,
        [SaleStatus] int NOT NULL,
        [FinalSalePrice] decimal(18,2) NULL,
        [DateSold] datetime2 NULL,
        [ReceiptNumber] nvarchar(30) NULL,
        [BuyerName] nvarchar(150) NULL,
        [BuyerPhone] nvarchar(20) NULL,
        [BuyerEmail] nvarchar(150) NULL,
        [BuyerAddress] nvarchar(300) NULL,
        [CommissionAmount] decimal(18,2) NULL,
        [NetPayableToStudent] decimal(18,2) NULL,
        [PaymentStatus] int NOT NULL,
        [AmountPaidToStudent] decimal(18,2) NULL,
        [PaymentDate] datetime2 NULL,
        [PaymentRemarks] nvarchar(500) NULL,
        [Notes] nvarchar(1000) NULL,
        [ListedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ExhibitionListings] PRIMARY KEY ([ExhibitionListingId]),
        CONSTRAINT [FK_ExhibitionListings_AspNetUsers_ForwardedById] FOREIGN KEY ([ForwardedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExhibitionListings_Exhibitions_ExhibitionId] FOREIGN KEY ([ExhibitionId]) REFERENCES [Exhibitions] ([ExhibitionId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExhibitionListings_Submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [Submissions] ([SubmissionId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE TABLE [PaintingLikes] (
        [SubmissionId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PaintingLikes] PRIMARY KEY ([SubmissionId], [UserId]),
        CONSTRAINT [FK_PaintingLikes_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaintingLikes_Submissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [Submissions] ([SubmissionId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Announcements_CreatedById] ON [Announcements] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Awards_CertificateNumber] ON [Awards] ([CertificateNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Awards_CompetitionAwardId] ON [Awards] ([CompetitionAwardId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Awards_CompetitionId_CompetitionAwardId] ON [Awards] ([CompetitionId], [CompetitionAwardId]) WHERE [CompetitionAwardId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Awards_IssuedById] ON [Awards] ([IssuedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Awards_StudentId] ON [Awards] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Awards_SubmissionId] ON [Awards] ([SubmissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_CompetitionAwards_CompetitionId] ON [CompetitionAwards] ([CompetitionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Competitions_CreatedById] ON [Competitions] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Competitions_Status] ON [Competitions] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Evaluations_EvaluatedById] ON [Evaluations] ([EvaluatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Evaluations_SubmissionId] ON [Evaluations] ([SubmissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExhibitionListings_ExhibitionId_SubmissionId] ON [ExhibitionListings] ([ExhibitionId], [SubmissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_ExhibitionListings_ForwardedById] ON [ExhibitionListings] ([ForwardedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_ExhibitionListings_SubmissionId] ON [ExhibitionListings] ([SubmissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Exhibitions_CreatedById] ON [Exhibitions] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_PaintingLikes_UserId] ON [PaintingLikes] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Staff_ApplicationUserId] ON [Staff] ([ApplicationUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Students_ApplicationUserId] ON [Students] ([ApplicationUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Students_EnrollmentNumber] ON [Students] ([EnrollmentNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Submissions_CompetitionId] ON [Submissions] ([CompetitionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE INDEX [IX_Submissions_Grade] ON [Submissions] ([Grade]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Submissions_StudentId_CompetitionId] ON [Submissions] ([StudentId], [CompetitionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919215221_Initial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260919215221_Initial', N'8.0.24');
END;
GO

COMMIT;
GO

