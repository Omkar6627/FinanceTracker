import { MemberRole } from '../models/api.models';

export const Permissions = {
  TransactionCreate: 'transaction.create',
  TransactionApprove: 'transaction.approve',
  TransactionDelete: 'transaction.delete',
  BudgetManage: 'budget.manage',
  ReportView: 'report.view',
  MemberInvite: 'member.invite',
  MemberManage: 'member.manage',
  DepartmentManage: 'department.manage',
  AuditView: 'audit.view',
  SettingsManage: 'settings.manage',
  OrganisationModeSwitch: 'organisation.mode.switch',
} as const;

export type Permission = typeof Permissions[keyof typeof Permissions];

const matrix: Record<string, MemberRole[]> = {
  [Permissions.TransactionCreate]:      ['Owner', 'Admin', 'Manager', 'Member'],
  [Permissions.TransactionApprove]:     ['Owner', 'Admin', 'Manager'],
  [Permissions.TransactionDelete]:      ['Owner', 'Admin', 'Member'],
  [Permissions.BudgetManage]:           ['Owner', 'Admin', 'Manager'],
  [Permissions.ReportView]:             ['Owner', 'Admin', 'Manager', 'Member', 'Viewer'],
  [Permissions.MemberInvite]:           ['Owner', 'Admin'],
  [Permissions.MemberManage]:           ['Owner', 'Admin'],
  [Permissions.DepartmentManage]:       ['Owner', 'Admin'],
  [Permissions.AuditView]:              ['Owner', 'Admin'],
  [Permissions.SettingsManage]:         ['Owner', 'Admin'],
  [Permissions.OrganisationModeSwitch]: ['Owner'],
};

export function canRole(role: MemberRole | null | undefined, permission: string): boolean {
  if (!role) return false;
  return matrix[permission]?.includes(role) ?? false;
}
