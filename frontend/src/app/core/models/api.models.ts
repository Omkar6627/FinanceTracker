export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  organisationId: string;
  organisationName: string;
  organisationMode: 'Individual' | 'Enterprise';
  currency: string;
  role: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserProfile;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  currency?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export type TxType = 'Income' | 'Expense' | 'Transfer';
export type TxStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'Rejected' | 'AutoApproved';

export interface Category {
  id: string;
  name: string;
  icon: string;
  color: string;
  type: 'Income' | 'Expense';
  isSystem: boolean;
}

export interface TransactionItem {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  amount: number;
  type: TxType;
  status: TxStatus;
  note: string;
  date: string;
  submittedByUserId: string;
  submittedByName: string;
}

export interface TransactionDetail extends TransactionItem {
  accountId?: string | null;
  accountName?: string | null;
  source: string;
}

export interface TransactionListResponse {
  items: TransactionItem[];
  page: number;
  pageSize: number;
  total: number;
}

export interface CreateTransactionRequest {
  categoryId: string;
  accountId?: string | null;
  amount: number;
  type: TxType;
  note?: string | null;
  date: string;
}

export interface UpdateTransactionRequest extends CreateTransactionRequest {}

export interface CategorySlice {
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  amount: number;
  percent: number;
}

export interface RecentTransaction {
  id: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  amount: number;
  type: TxType;
  note: string;
  date: string;
}

export interface DashboardSummary {
  incomeMonth: number;
  expenseMonth: number;
  netMonth: number;
  transactionCountMonth: number;
  incomeToday: number;
  expenseToday: number;
  topCategories: CategorySlice[];
  recentTransactions: RecentTransaction[];
}

export interface DayPoint {
  date: string;
  income: number;
  expense: number;
}

export interface MonthlyReport {
  year: number;
  month: number;
  totalIncome: number;
  totalExpense: number;
  net: number;
  dailySeries: DayPoint[];
  expenseByCategory: CategorySlice[];
  incomeByCategory: CategorySlice[];
}

export interface TrendPoint {
  year: number;
  month: number;
  income: number;
  expense: number;
  net: number;
}

export interface TrendReport {
  points: TrendPoint[];
}

export interface Budget {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  limitAmount: number;
  period: 'Weekly' | 'Monthly' | 'Custom';
  startDate: string;
  endDate?: string | null;
}

export interface BudgetStatus {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  limitAmount: number;
  spentAmount: number;
  remainingAmount: number;
  percentUsed: number;
  period: string;
  periodStart: string;
  periodEnd: string;
}

export interface CreateBudgetRequest {
  categoryId: string;
  limitAmount: number;
  period: 'Weekly' | 'Monthly' | 'Custom';
  startDate?: string | null;
  endDate?: string | null;
}

export interface ApiError {
  message: string;
  errors?: string[];
}

// ------- Phase 2 enterprise types -------

export type MemberRole = 'Owner' | 'Admin' | 'Manager' | 'Member' | 'Viewer';

export interface OrganisationInfo {
  id: string;
  name: string;
  mode: 'Individual' | 'Enterprise';
  currency: string;
  memberCount: number;
  departmentCount: number;
}

export interface UpdateOrganisationRequest {
  name: string;
  currency: string;
}

export interface SwitchModeRequest {
  mode: 'Individual' | 'Enterprise';
}

export interface DepartmentDto {
  id: string;
  name: string;
  managerMemberId?: string | null;
  managerName?: string | null;
  memberCount: number;
}

export interface CreateDepartmentRequest {
  name: string;
  managerMemberId?: string | null;
}

export interface UpdateDepartmentRequest extends CreateDepartmentRequest {}

export interface MemberDto {
  id: string;
  userId: string;
  email: string;
  fullName: string;
  role: MemberRole;
  departmentId?: string | null;
  departmentName?: string | null;
  isActive: boolean;
  joinedAt: string;
}

export interface InvitationDto {
  id: string;
  email: string;
  role: MemberRole;
  departmentId?: string | null;
  departmentName?: string | null;
  status: 'Pending' | 'Accepted' | 'Expired' | 'Revoked';
  expiresAt: string;
  token: string;
}

export interface InviteMemberRequest {
  email: string;
  role: MemberRole;
  departmentId?: string | null;
}

export interface ChangeRoleRequest {
  role: MemberRole;
  departmentId?: string | null;
}

export interface AcceptInvitationRequest {
  token: string;
  fullName: string;
  password: string;
}

export interface RejectTransactionRequest {
  reason: string;
}

export interface AuditEntry {
  id: string;
  actorUserId: string;
  actorName: string;
  action: string;
  entityType: string;
  entityId: string;
  oldValue?: string | null;
  newValue?: string | null;
  occurredAt: string;
}

export interface AuditListResponse {
  items: AuditEntry[];
  page: number;
  pageSize: number;
  total: number;
}

export interface DepartmentSummary {
  departmentId?: string | null;
  departmentName: string;
  income: number;
  expense: number;
  net: number;
  transactionCount: number;
}
