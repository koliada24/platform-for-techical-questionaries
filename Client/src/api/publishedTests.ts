import { api } from './client';

export interface PublishedTestInfoDto {
  id: string;
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  questionCount: number;
  closesAt: string;
}

export const publishedTestsApi = {
  getInfo: (id: string) =>
    api.get<PublishedTestInfoDto>(`/api/published-tests/${id}/info`).then((r) => r.data),
};
