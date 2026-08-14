import { Pipe, PipeTransform } from '@angular/core';

/** Turns a PascalCase status like "AwaitingApproval" into "Awaiting Approval" for display. */
@Pipe({ name: 'statusLabel' })
export class StatusLabelPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return value ? value.replace(/([a-z])([A-Z])/g, '$1 $2') : '';
  }
}
