import { Pipe, PipeTransform } from '@angular/core';

/**
 * Splits a PascalCase code into spaced words for display.
 *
 *   VoiceReviewSentBack -> Voice Review Sent Back
 *   DueDateChanged      -> Due Date Changed
 *
 * Timeline action types are stored as single-word codes so they stay stable to
 * compare against; only what the user reads is spaced out.
 */
@Pipe({ name: 'humanize', standalone: true })
export class HumanizePipe implements PipeTransform {
  public transform(value?: string | null): string {
    if (!value) return '';
    return (
      value
        // aB -> a B, and 3D -> 3 D
        .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
        // keeps runs of capitals together: PFCReport -> PFC Report
        .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
        .trim()
    );
  }
}
