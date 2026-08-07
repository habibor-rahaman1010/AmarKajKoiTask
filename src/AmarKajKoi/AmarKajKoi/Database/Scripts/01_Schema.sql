-- =============================================================
-- Amar Kaj Koi - Database Schema (UNIQUEIDENTIFIER / GUID PKs)
-- Database: AmarKajKoiDB
-- =============================================================

-- Drop database if it exists (so a re-run gets a clean schema)
IF DB_ID('AmarKajKoiDB') IS NOT NULL
BEGIN
    ALTER DATABASE AmarKajKoiDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE AmarKajKoiDB;
END
GO

CREATE DATABASE AmarKajKoiDB;
GO

USE AmarKajKoiDB;
GO

-- ============================================================
-- Roles
-- ============================================================
CREATE TABLE dbo.Roles
(
    RoleId       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    RoleName     NVARCHAR(50)     NOT NULL UNIQUE,
    Description  NVARCHAR(200)    NULL,
    CreatedAt    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- ============================================================
-- Users
-- ============================================================
CREATE TABLE dbo.Users
(
    UserId       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    FullName     NVARCHAR(150)    NOT NULL,
    Email        NVARCHAR(150)    NOT NULL UNIQUE,
    Username     NVARCHAR(80)     NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500)    NOT NULL,
    RoleId       UNIQUEIDENTIFIER NOT NULL,
    IsActive     BIT              NOT NULL DEFAULT(1),
    CreatedAt    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt    DATETIME2        NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO

-- ============================================================
-- Task Centers  (Production / Commercial / IE)
-- ============================================================
CREATE TABLE dbo.TaskCenters
(
    TaskCenterId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name         NVARCHAR(100)    NOT NULL UNIQUE,
    IsActive     BIT              NOT NULL DEFAULT(1)
);
GO

-- ============================================================
-- Event Channels
-- ============================================================
CREATE TABLE dbo.EventChannels
(
    EventChannelId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name           NVARCHAR(100)    NOT NULL UNIQUE,
    IsActive       BIT              NOT NULL DEFAULT(1)
);
GO

-- ============================================================
-- Day Events
-- ============================================================
CREATE TABLE dbo.DayEvents
(
    DayEventId     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name           NVARCHAR(150)    NOT NULL,
    EventChannelId UNIQUEIDENTIFIER NULL,
    EventDate      DATE             NULL,
    IsActive       BIT              NOT NULL DEFAULT(1),
    CONSTRAINT FK_DayEvents_EventChannels FOREIGN KEY (EventChannelId) REFERENCES dbo.EventChannels(EventChannelId)
);
GO

-- ============================================================
-- Task Statuses (lookup) - GUID PK, unique StatusCode for lookup
-- ============================================================
CREATE TABLE dbo.TaskStatuses
(
    StatusId   UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    StatusCode NVARCHAR(50)     NOT NULL UNIQUE,
    StatusName NVARCHAR(80)     NOT NULL
);
GO

-- ============================================================
-- Tasks
-- ============================================================
CREATE TABLE dbo.Tasks
(
    TaskId           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    TaskName         NVARCHAR(300)    NOT NULL,
    TaskType         NVARCHAR(30)     NOT NULL, -- 'Target' or 'Commitment'
    TaskCenterId     UNIQUEIDENTIFIER NULL,
    EventChannelId   UNIQUEIDENTIFIER NULL,
    DayEventId       UNIQUEIDENTIFIER NULL,
    CreatedByUserId  UNIQUEIDENTIFIER NOT NULL,
    AssignedToUserId UNIQUEIDENTIFIER NULL,
    DueDate          DATETIME2        NULL,
    StatusId         UNIQUEIDENTIFIER NOT NULL,
    RequestCount     INT              NOT NULL DEFAULT(0),
    IsPinned         BIT              NOT NULL DEFAULT(0),
    VoiceFileId      UNIQUEIDENTIFIER NULL,
    VoiceAutoText    NVARCHAR(MAX)    NULL,
    Description      NVARCHAR(MAX)    NULL,
    FinalComment     NVARCHAR(1000)   NULL,
    RowVersion       ROWVERSION       NOT NULL,
    CreatedAt        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt        DATETIME2        NULL,
    CONSTRAINT FK_Tasks_TaskCenters   FOREIGN KEY (TaskCenterId)     REFERENCES dbo.TaskCenters(TaskCenterId),
    CONSTRAINT FK_Tasks_EventChannels FOREIGN KEY (EventChannelId)   REFERENCES dbo.EventChannels(EventChannelId),
    CONSTRAINT FK_Tasks_DayEvents     FOREIGN KEY (DayEventId)       REFERENCES dbo.DayEvents(DayEventId),
    CONSTRAINT FK_Tasks_CreatedBy     FOREIGN KEY (CreatedByUserId)  REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Tasks_AssignedTo    FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Tasks_Statuses      FOREIGN KEY (StatusId)         REFERENCES dbo.TaskStatuses(StatusId)
);
GO

-- ============================================================
-- Voice Files
-- ============================================================
CREATE TABLE dbo.VoiceFiles
(
    VoiceFileId      UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    TaskId           UNIQUEIDENTIFIER NULL,
    UploadedByUserId UNIQUEIDENTIFIER NOT NULL,
    FileName         NVARCHAR(300)    NOT NULL,
    StoragePath      NVARCHAR(500)    NOT NULL,
    DurationSecs     INT              NOT NULL DEFAULT(0),
    Purpose          NVARCHAR(50)     NOT NULL, -- 'Target', 'CommitmentVoice', 'ExtendReason'
    TranscribedText  NVARCHAR(MAX)    NULL,
    CreatedAt        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_VoiceFiles_Users FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_VoiceFiles_Tasks FOREIGN KEY (TaskId)           REFERENCES dbo.Tasks(TaskId)
);
GO

-- ============================================================
-- Extend / Revise Requests
-- ============================================================
CREATE TABLE dbo.ExtendRequests
(
    RequestId          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    TaskId             UNIQUEIDENTIFIER NOT NULL,
    RequestedByUserId  UNIQUEIDENTIFIER NOT NULL,
    RequestType        NVARCHAR(20)     NOT NULL, -- 'Extend' or 'Revise'
    RequestedDueDate   DATETIME2        NULL,
    ReasonText         NVARCHAR(1000)   NULL,
    VoiceFileId        UNIQUEIDENTIFIER NULL,
    Status             NVARCHAR(20)     NOT NULL DEFAULT('Pending'),
    DecisionByUserId   UNIQUEIDENTIFIER NULL,
    DecisionReason     NVARCHAR(1000)   NULL,
    CreatedAt          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    DecidedAt          DATETIME2        NULL,
    CONSTRAINT FK_ExtendRequests_Tasks FOREIGN KEY (TaskId)            REFERENCES dbo.Tasks(TaskId),
    CONSTRAINT FK_ExtendRequests_ReqBy FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_ExtendRequests_DecBy FOREIGN KEY (DecisionByUserId)  REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_ExtendRequests_Voice FOREIGN KEY (VoiceFileId)       REFERENCES dbo.VoiceFiles(VoiceFileId)
);
GO

-- ============================================================
-- Task Timeline / Activity Log
-- ============================================================
CREATE TABLE dbo.TaskTimeline
(
    TimelineId     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    TaskId         UNIQUEIDENTIFIER NOT NULL,
    ActionByUserId UNIQUEIDENTIFIER NOT NULL,
    ActionType     NVARCHAR(80)     NOT NULL,
    FromStatusId   UNIQUEIDENTIFIER NULL,
    ToStatusId     UNIQUEIDENTIFIER NULL,
    Note           NVARCHAR(2000)   NULL,
    CreatedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_TaskTimeline_Tasks FOREIGN KEY (TaskId)         REFERENCES dbo.Tasks(TaskId),
    CONSTRAINT FK_TaskTimeline_User  FOREIGN KEY (ActionByUserId) REFERENCES dbo.Users(UserId)
);
GO

-- ============================================================
-- Notifications
-- ============================================================
CREATE TABLE dbo.Notifications
(
    NotificationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId         UNIQUEIDENTIFIER NOT NULL,
    TaskId         UNIQUEIDENTIFIER NULL,
    Title          NVARCHAR(200)    NOT NULL,
    Body           NVARCHAR(1000)   NULL,
    IsRead         BIT              NOT NULL DEFAULT(0),
    CreatedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Notifications_Tasks FOREIGN KEY (TaskId) REFERENCES dbo.Tasks(TaskId)
);
GO

-- ============================================================
-- Indexes
-- ============================================================
CREATE INDEX IX_Tasks_Status         ON dbo.Tasks(StatusId);
CREATE INDEX IX_Tasks_Assigned       ON dbo.Tasks(AssignedToUserId);
CREATE INDEX IX_Tasks_DueDate        ON dbo.Tasks(DueDate);
CREATE INDEX IX_Timeline_Task        ON dbo.TaskTimeline(TaskId, CreatedAt);
CREATE INDEX IX_Notifications_User   ON dbo.Notifications(UserId, IsRead);
GO
