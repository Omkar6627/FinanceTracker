import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Directive({
  selector: '[hasPermission]',
  standalone: false,
})
export class HasPermissionDirective {
  private rendered = false;

  constructor(
    private template: TemplateRef<unknown>,
    private container: ViewContainerRef,
    private auth: AuthService,
  ) {}

  @Input() set hasPermission(permission: string) {
    const allowed = this.auth.can(permission);
    if (allowed && !this.rendered) {
      this.container.createEmbeddedView(this.template);
      this.rendered = true;
    } else if (!allowed && this.rendered) {
      this.container.clear();
      this.rendered = false;
    }
  }
}
