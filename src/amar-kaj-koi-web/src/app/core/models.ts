export interface UserProfile {
  userId: string;
  fullName: string;
  username: string;
  email: string;
  roleName: string;
  roleId: string;
}

export interface LoginResponse {
  token: string;
  user: UserProfile;
}

export interface TaskListItem {
  taskId: string;
  taskName: string;
  taskType: string;
  taskCenter?: string;
  eventChannel?: string;
  dayEvent?: string;
  assignedToName?: string;
  assignedToUserId?: string;
  createdByName?: string;
  createdByUserId: string;
  dueDate?: string;
  statusCode: string;
  statusName: string;
  statusId: string;
  requestCount: number;
  isPinned: boolean;
  createdAt: string;
}

export interface TaskTimelineEntry {
  timelineId: string;
  actionType: string;
  actionByName: string;
  fromStatus?: string;
  toStatus?: string;
  note?: string;
  createdAt: string;
}

export interface ExtendRequest {
  requestId: string;
  taskId: string;
  requestedByName: string;
  requestType: string;
  requestedDueDate?: string;
  reasonText?: string;
  voiceFileId?: string;
  status: string;
  decisionByName?: string;
  decisionReason?: string;
  createdAt: string;
  decidedAt?: string;
}

export interface TaskDetail extends TaskListItem {
  description?: string;
  voiceAutoText?: string;
  voiceFileId?: string;
  finalComment?: string;
  timeline: TaskTimelineEntry[];
  extendRequests: ExtendRequest[];
}

export interface TaskCenterRef {
  taskCenterId: string;
  name: string;
}
export interface EventChannelRef {
  eventChannelId: string;
  name: string;
}
export interface DayEventRef {
  dayEventId: string;
  name: string;
  eventChannelId?: string;
}
export interface StatusRef {
  statusId: string;
  statusCode: string;
  statusName: string;
}
export interface EmployeeRef {
  userId: string;
  fullName: string;
  username: string;
  roleName: string;
}

export interface NotificationDto {
  notificationId: string;
  taskId?: string;
  title: string;
  body?: string;
  isRead: boolean;
  createdAt: string;
}

export interface PerformanceRow {
  userId: string;
  fullName: string;
  total: number;
  passed: number;
  failed: number;
  cancelled: number;
  overdue: number;
  open: number;
  passRate: number;
}

export interface VoiceUploadResponse {
  voiceFileId: string;
  fileName: string;
  durationSecs: number;
}

export type UserRole = 'TopManagement' | 'Employee' | 'VoiceReviewer' | 'SystemAdmin';