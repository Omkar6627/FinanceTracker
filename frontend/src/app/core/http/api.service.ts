import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuditListResponse,
  Budget,
  BudgetStatus,
  Category,
  ChangeRoleRequest,
  CreateBudgetRequest,
  CreateDepartmentRequest,
  CreateRecurringRule,
  CreateTransactionRequest,
  CsvImportResult,
  DashboardSummary,
  FxRates,
  RecurringRule,
  UpdateRecurringRule,
  DepartmentDto,
  DepartmentSummary,
  InvitationDto,
  InviteMemberRequest,
  MemberDto,
  MonthlyReport,
  OrganisationInfo,
  RejectTransactionRequest,
  SwitchModeRequest,
  TransactionDetail,
  TransactionListResponse,
  TrendReport,
  UpdateDepartmentRequest,
  UpdateOrganisationRequest,
  UpdateTransactionRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  listCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.base}/categories`);
  }

  createCategory(body: { name: string; icon: string; color: string; type: 'Income' | 'Expense' }): Observable<Category> {
    return this.http.post<Category>(`${this.base}/categories`, body);
  }

  listTransactions(opts: {
    from?: string;
    to?: string;
    categoryId?: string;
    type?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  } = {}): Observable<TransactionListResponse> {
    let params = new HttpParams();
    Object.entries(opts).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v));
    });
    return this.http.get<TransactionListResponse>(`${this.base}/transactions`, { params });
  }

  listPendingTransactions(page = 1, pageSize = 30): Observable<TransactionListResponse> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<TransactionListResponse>(`${this.base}/transactions/pending`, { params });
  }

  approveTransaction(id: string): Observable<TransactionDetail> {
    return this.http.post<TransactionDetail>(`${this.base}/transactions/${id}/approve`, {});
  }

  rejectTransaction(id: string, body: RejectTransactionRequest): Observable<TransactionDetail> {
    return this.http.post<TransactionDetail>(`${this.base}/transactions/${id}/reject`, body);
  }

  getTransaction(id: string): Observable<TransactionDetail> {
    return this.http.get<TransactionDetail>(`${this.base}/transactions/${id}`);
  }

  createTransaction(body: CreateTransactionRequest): Observable<TransactionDetail> {
    return this.http.post<TransactionDetail>(`${this.base}/transactions`, body);
  }

  updateTransaction(id: string, body: UpdateTransactionRequest): Observable<TransactionDetail> {
    return this.http.put<TransactionDetail>(`${this.base}/transactions/${id}`, body);
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/transactions/${id}`);
  }

  exportTransactionsCsv(opts: { from?: string; to?: string; categoryId?: string; type?: string; status?: string } = {}): Observable<Blob> {
    let params = new HttpParams();
    Object.entries(opts).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v));
    });
    return this.http.get(`${this.base}/transactions/export.csv`, { params, responseType: 'blob' });
  }

  importTransactionsCsv(file: File, commit: boolean): Observable<CsvImportResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    const params = new HttpParams().set('commit', String(commit));
    return this.http.post<CsvImportResult>(`${this.base}/transactions/import/csv`, form, { params });
  }

  // -------- Recurring --------
  listRecurring(): Observable<RecurringRule[]> {
    return this.http.get<RecurringRule[]>(`${this.base}/recurring`);
  }

  createRecurring(body: CreateRecurringRule): Observable<RecurringRule> {
    return this.http.post<RecurringRule>(`${this.base}/recurring`, body);
  }

  updateRecurring(id: string, body: UpdateRecurringRule): Observable<RecurringRule> {
    return this.http.put<RecurringRule>(`${this.base}/recurring/${id}`, body);
  }

  deleteRecurring(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/recurring/${id}`);
  }

  runRecurring(id: string): Observable<RecurringRule> {
    return this.http.post<RecurringRule>(`${this.base}/recurring/${id}/run`, {});
  }

  // -------- FX --------
  getFxRates(baseCurrency: string): Observable<FxRates> {
    const params = new HttpParams().set('base', baseCurrency);
    return this.http.get<FxRates>(`${this.base}/fx/rates`, { params });
  }

  listBudgets(): Observable<Budget[]> {
    return this.http.get<Budget[]>(`${this.base}/budgets`);
  }

  getBudgetStatus(): Observable<BudgetStatus[]> {
    return this.http.get<BudgetStatus[]>(`${this.base}/budgets/status`);
  }

  createBudget(body: CreateBudgetRequest): Observable<Budget> {
    return this.http.post<Budget>(`${this.base}/budgets`, body);
  }

  deleteBudget(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/budgets/${id}`);
  }

  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.base}/reports/dashboard`);
  }

  getMonthly(year: number, month: number): Observable<MonthlyReport> {
    const params = new HttpParams().set('year', year).set('month', month);
    return this.http.get<MonthlyReport>(`${this.base}/reports/monthly`, { params });
  }

  downloadMonthlyPdf(year: number, month: number): Observable<Blob> {
    const params = new HttpParams().set('year', year).set('month', month);
    return this.http.get(`${this.base}/reports/monthly.pdf`, { params, responseType: 'blob' });
  }

  getTrends(months = 6): Observable<TrendReport> {
    const params = new HttpParams().set('months', months);
    return this.http.get<TrendReport>(`${this.base}/reports/trends`, { params });
  }

  getDepartmentReport(from?: string, to?: string): Observable<DepartmentSummary[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<DepartmentSummary[]>(`${this.base}/reports/departments`, { params });
  }

  // -------- Organisation --------
  getOrganisation(): Observable<OrganisationInfo> {
    return this.http.get<OrganisationInfo>(`${this.base}/organisation`);
  }

  updateOrganisation(body: UpdateOrganisationRequest): Observable<OrganisationInfo> {
    return this.http.put<OrganisationInfo>(`${this.base}/organisation`, body);
  }

  switchOrganisationMode(body: SwitchModeRequest): Observable<OrganisationInfo> {
    return this.http.put<OrganisationInfo>(`${this.base}/organisation/mode`, body);
  }

  // -------- Departments --------
  listDepartments(): Observable<DepartmentDto[]> {
    return this.http.get<DepartmentDto[]>(`${this.base}/departments`);
  }

  createDepartment(body: CreateDepartmentRequest): Observable<DepartmentDto> {
    return this.http.post<DepartmentDto>(`${this.base}/departments`, body);
  }

  updateDepartment(id: string, body: UpdateDepartmentRequest): Observable<DepartmentDto> {
    return this.http.put<DepartmentDto>(`${this.base}/departments/${id}`, body);
  }

  deleteDepartment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/departments/${id}`);
  }

  // -------- Members --------
  listMembers(): Observable<MemberDto[]> {
    return this.http.get<MemberDto[]>(`${this.base}/members`);
  }

  listInvitations(): Observable<InvitationDto[]> {
    return this.http.get<InvitationDto[]>(`${this.base}/members/invitations`);
  }

  inviteMember(body: InviteMemberRequest): Observable<InvitationDto> {
    return this.http.post<InvitationDto>(`${this.base}/members/invite`, body);
  }

  revokeInvitation(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/members/invitations/${id}`);
  }

  changeMemberRole(id: string, body: ChangeRoleRequest): Observable<MemberDto> {
    return this.http.put<MemberDto>(`${this.base}/members/${id}/role`, body);
  }

  removeMember(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/members/${id}`);
  }

  // -------- Audit --------
  listAudit(opts: { from?: string; to?: string; actorId?: string; action?: string; page?: number; pageSize?: number } = {}): Observable<AuditListResponse> {
    let params = new HttpParams();
    Object.entries(opts).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v));
    });
    return this.http.get<AuditListResponse>(`${this.base}/audit`, { params });
  }
}
