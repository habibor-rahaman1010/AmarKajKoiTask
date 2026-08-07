import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  DayEventRef,
  EmployeeRef,
  EventChannelRef,
  ExtendRequest,
  NotificationDto,
  PerformanceRow,
  StatusRef,
  TaskCenterRef,
  TaskDetail,
  TaskListItem,
  VoiceUploadResponse,
} from './models';

export interface TaskFilter {
  statusId?: string;
  fromDate?: string;
  toDate?: string;
  assignedToUserId?: string;
  createdByUserId?: string;
  eventChannelId?: string;
  search?: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // ---- Reference ----
  public taskCenters(): Observable<TaskCenterRef[]> {
    return this.http.get<TaskCenterRef[]>(`${this.base}/reference/task-centers`);
  }

  public eventChannels(): Observable<EventChannelRef[]> {
    return this.http.get<EventChannelRef[]>(`${this.base}/reference/event-channels`);
  }

  public dayEvents(channelId?: string): Observable<DayEventRef[]> {
    let params = new HttpParams();
    if (channelId) params = params.set('eventChannelId', channelId);
    return this.http.get<DayEventRef[]>(`${this.base}/reference/day-events`, { params });
  }

  public statuses(): Observable<StatusRef[]> {
    return this.http.get<StatusRef[]>(`${this.base}/reference/statuses`);
  }

  public employees(): Observable<EmployeeRef[]> {
    return this.http.get<EmployeeRef[]>(`${this.base}/reference/employees`);
  }

  public allUsers(): Observable<EmployeeRef[]> {
    return this.http.get<EmployeeRef[]>(`${this.base}/reference/users`);
  }

  // ---- Tasks ----
  public listTasks(f: TaskFilter): Observable<TaskListItem[]> {
    let params = new HttpParams();
    Object.entries(f || {}).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') {
        params = params.set(k, v as any);
      }
    });
    return this.http.get<TaskListItem[]>(`${this.base}/tasks`, { params });
  }

  public listAllTasks(f: TaskFilter): Observable<TaskListItem[]> {
    let params = new HttpParams();
    Object.entries(f || {}).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, v as any);
    });
    return this.http.get<TaskListItem[]>(`${this.base}/tasks/all`, { params });
  }

  public listMyCreatedTasks(f: TaskFilter = {}): Observable<TaskListItem[]> {
    let params = new HttpParams();
    Object.entries(f || {}).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, v as any);
    });
    return this.http.get<TaskListItem[]>(`${this.base}/tasks/mine`, { params });
  }

  public getTask(id: string): Observable<TaskDetail> {
    return this.http.get<TaskDetail>(`${this.base}/tasks/${id}`);
  }

  public createTarget(data: any): Observable<{ taskId: string }> {
    return this.http.post<{ taskId: string }>(`${this.base}/tasks/target`, data);
  }

  public createCommitment(data: any): Observable<{ taskId: string }> {
    return this.http.post<{ taskId: string }>(`${this.base}/tasks/commitment`, data);
  }

  public editCommitment(data: any): Observable<void> {
    return this.http.put<void>(`${this.base}/tasks/commitment`, data);
  }

  public editTarget(data: any): Observable<void> {
    return this.http.put<void>(`${this.base}/tasks/target`, data);
  }

  public postTask(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/${id}/post`, {});
  }

  public deleteDraft(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/tasks/${id}`);
  }

  public approveTask(taskId: string, assignedToUserId?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/approve`, { taskId, assignedToUserId });
  }

  public rejectTask(taskId: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/reject`, { taskId, reason });
  }

  public sendBackTask(taskId: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/send-back`, { taskId, reason });
  }

  public voiceReviewComplete(data: any): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/voice-review/complete`, data);
  }

  public voiceReviewSendBack(taskId: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/voice-review/send-back`, { taskId, reason });
  }

  public requestExtend(data: any): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/extend-request`, data);
  }

  public pendingExtends(): Observable<ExtendRequest[]> {
    return this.http.get<ExtendRequest[]>(`${this.base}/tasks/extend-request/pending`);
  }

  public decideExtend(requestId: string, approve: boolean, reason?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/extend-request/decide`, {
      requestId,
      approve,
      reason,
    });
  }

  public requestMarkPassed(taskId: string, note?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/request-mark-passed`, { taskId, note });
  }

  public markFinal(
    taskId: string,
    decision: 'Passed' | 'Failed' | 'Cancelled',
    comment?: string,
  ): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/final`, { taskId, decision, comment });
  }

  public bulkFinal(
    taskIds: string[],
    decision: 'Passed' | 'Failed' | 'Cancelled',
    comment?: string,
  ): Observable<{ affected: number }> {
    return this.http.post<{ affected: number }>(`${this.base}/tasks/bulk-final`, {
      taskIds,
      decision,
      comment,
    });
  }

  public changeDueDate(taskId: string, newDueDate: string, note?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/due-date`, { taskId, newDueDate, note });
  }

  public changeAssignee(taskId: string, newAssigneeUserId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/assignee`, { taskId, newAssigneeUserId });
  }

  public pinTask(taskId: string, pinned: boolean): Observable<void> {
    let params = new HttpParams().set('pinned', pinned);
    return this.http.post<void>(`${this.base}/tasks/${taskId}/pin`, {}, { params });
  }

  public attachVoiceCommitment(taskId: string, voiceFileId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/${taskId}/voice-commitment/${voiceFileId}`, {});
  }

  public runOverdueSweep(): Observable<{ affected: number }> {
    return this.http.post<{ affected: number }>(`${this.base}/tasks/run-overdue-sweep`, {});
  }

  public reopenTask(taskId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/${taskId}/reopen`, {});
  }

  // ---- Performance ----
  public selfPerformance(): Observable<PerformanceRow> {
    return this.http.get<PerformanceRow>(`${this.base}/tasks/performance/self`);
  }

  public allPerformance(): Observable<PerformanceRow[]> {
    return this.http.get<PerformanceRow[]>(`${this.base}/tasks/performance/all`);
  }

  // ---- Notifications ----
  public notifications(onlyUnread: boolean): Observable<NotificationDto[]> {
    let params = new HttpParams().set('onlyUnread', onlyUnread);
    return this.http.get<NotificationDto[]>(`${this.base}/notifications`, { params });
  }

  /** Seeds the badge on load; after that the notifications hub keeps it current. */
  public unreadNotifCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.base}/notifications/unread-count`);
  }

  public markNotifRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/notifications/${id}/read`, {});
  }

  public markAllNotifRead(): Observable<void> {
    return this.http.post<void>(`${this.base}/notifications/read-all`, {});
  }

  // ---- Voice ----
  public uploadVoice(
    file: Blob,
    filename: string,
    purpose: string,
    durationSecs: number,
  ): Observable<VoiceUploadResponse> {
    const fd = new FormData();
    fd.append('file', file, filename);
    fd.append('purpose', purpose);
    fd.append('durationSecs', String(durationSecs));
    return this.http.post<VoiceUploadResponse>(`${this.base}/voice/upload`, fd);
  }

  /**
   * Playback has to pull the bytes down here rather than pointing an <audio src>
   * at the endpoint: /api/voice/{id} is [Authorize]d and the bearer token is only
   * attached by the HTTP interceptor, so a browser-issued media request carries
   * no credentials and comes back 401. Callers wrap the blob in an object URL —
   * see VoicePlayerComponent.
   */
  public voiceBlob(voiceFileId: string): Observable<Blob> {
    return this.http.get(`${this.base}/voice/${voiceFileId}`, { responseType: 'blob' });
  }

  // ---- Users ----
  public register(data: any): Observable<any> {
    return this.http.post(`${this.base}/auth/register`, data);
  }
}