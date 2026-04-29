import { api } from './client';

export interface AttemptInProgressDto {
  id: string;
  publishedTestId: string;
  startedAt: string;
}

export const attemptsApi = {
  start: (publishedTestId: string) =>
    api
      .post<AttemptInProgressDto>(`/api/published-tests/${publishedTestId}/attempts`)
      .then((r) => r.data),
};
