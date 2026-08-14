import { StatusLabelPipe } from './status-label.pipe';

describe('StatusLabelPipe', () => {
  const pipe = new StatusLabelPipe();

  it('inserts a space between a lowercase letter and the following uppercase letter', () => {
    expect(pipe.transform('AwaitingApproval')).toBe('Awaiting Approval');
  });

  it('leaves a single-word PascalCase status unchanged', () => {
    expect(pipe.transform('Confirmed')).toBe('Confirmed');
    expect(pipe.transform('Rejected')).toBe('Rejected');
    expect(pipe.transform('Cancelled')).toBe('Cancelled');
  });

  it('returns an empty string for null/undefined/empty input', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
    expect(pipe.transform('')).toBe('');
  });
});
