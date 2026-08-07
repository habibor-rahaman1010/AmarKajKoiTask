-- =============================================================
-- Amar Kaj Koi - Seed Data (GUID keys)
-- Uses deterministic GUIDs for lookup rows so C# constants match.
-- =============================================================
USE AmarKajKoiDB;
GO

-- ---- Roles (fixed GUIDs) ----
INSERT INTO dbo.Roles(RoleId, RoleName, Description) VALUES
  ('11111111-1111-1111-1111-000000000001', 'TopManagement', 'Top management role'),
  ('11111111-1111-1111-1111-000000000002', 'Employee',      'Regular employee role'),
  ('11111111-1111-1111-1111-000000000003', 'VoiceReviewer', 'Voice-to-Task reviewer'),
  ('11111111-1111-1111-1111-000000000004', 'SystemAdmin',   'System admin');
GO

-- ---- Task Statuses (fixed GUIDs) ----
INSERT INTO dbo.TaskStatuses(StatusId, StatusCode, StatusName) VALUES
  ('22222222-2222-2222-2222-000000000001', 'Draft',                      'Draft'),
  ('22222222-2222-2222-2222-000000000002', 'PendingManagementApproval',  'Pending Management Approval'),
  ('22222222-2222-2222-2222-000000000003', 'PendingVoiceReview',         'Pending Voice Review'),
  ('22222222-2222-2222-2222-000000000004', 'Open',                       'Open'),
  ('22222222-2222-2222-2222-000000000005', 'RequestToExtendRevise',      'Request to Extend/Revise'),
  ('22222222-2222-2222-2222-000000000006', 'Overdue',                    'Overdue'),
  ('22222222-2222-2222-2222-000000000007', 'Passed',                     'Passed'),
  ('22222222-2222-2222-2222-000000000008', 'Failed',                     'Failed'),
  ('22222222-2222-2222-2222-000000000009', 'Cancelled',                  'Cancelled');
GO

-- ---- Task Centers ----
INSERT INTO dbo.TaskCenters(Name) VALUES ('Production'), ('Commercial'), ('IE');
GO

-- ---- Event Channels ----
INSERT INTO dbo.EventChannels(Name) VALUES
  ('Weekly Production Meeting'),
  ('Commercial Meeting'),
  ('Fabric Meeting'),
  ('Others');
GO

-- ---- Day Events ----
DECLARE @WPM UNIQUEIDENTIFIER = (SELECT EventChannelId FROM dbo.EventChannels WHERE Name = 'Weekly Production Meeting');
DECLARE @CM  UNIQUEIDENTIFIER = (SELECT EventChannelId FROM dbo.EventChannels WHERE Name = 'Commercial Meeting');
DECLARE @FM  UNIQUEIDENTIFIER = (SELECT EventChannelId FROM dbo.EventChannels WHERE Name = 'Fabric Meeting');
DECLARE @OTH UNIQUEIDENTIFIER = (SELECT EventChannelId FROM dbo.EventChannels WHERE Name = 'Others');

INSERT INTO dbo.DayEvents(Name, EventChannelId) VALUES
  ('Monday Production Review',   @WPM),
  ('Wednesday Line Check',        @WPM),
  ('Friday Weekly Wrap-up',       @WPM),
  ('Order Booking Review',        @CM),
  ('Shipment Status Review',      @CM),
  ('Buyer Communication',         @CM),
  ('Fabric Sourcing Review',      @FM),
  ('Fabric Quality Check',        @FM),
  ('Fabric Inventory Review',     @FM),
  ('General Discussion',          @OTH),
  ('Ad-hoc Meeting',              @OTH);
GO

-- ---- Default users ----
-- PLAIN: prefix is upgraded to a proper hash after the first successful login.
DECLARE @Mgmt UNIQUEIDENTIFIER = (SELECT RoleId FROM dbo.Roles WHERE RoleName='TopManagement');
DECLARE @Emp  UNIQUEIDENTIFIER = (SELECT RoleId FROM dbo.Roles WHERE RoleName='Employee');
DECLARE @Rev  UNIQUEIDENTIFIER = (SELECT RoleId FROM dbo.Roles WHERE RoleName='VoiceReviewer');
DECLARE @Adm  UNIQUEIDENTIFIER = (SELECT RoleId FROM dbo.Roles WHERE RoleName='SystemAdmin');

INSERT INTO dbo.Users(FullName, Email, Username, PasswordHash, RoleId) VALUES
  ('System Admin',   'admin@amarkajkoi.local',    'admin',    'PLAIN:Admin@123',    @Adm),
  ('Top Manager',    'manager@amarkajkoi.local',  'manager',  'PLAIN:Manager@123',  @Mgmt),
  ('Employee One',   'employee@amarkajkoi.local', 'employee', 'PLAIN:Employee@123', @Emp),
  ('Voice Reviewer', 'reviewer@amarkajkoi.local', 'reviewer', 'PLAIN:Reviewer@123', @Rev);
GO
