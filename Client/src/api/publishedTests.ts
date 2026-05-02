import { api } from './client';

export interface PublishedTestInfoDto {
  id: string;
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  questionCount: number;
  closesAt: string;
}

export interface PublishedTestListItemDto {
  testTemplateId: string;
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  questionCount: number;
  courseCount: number;
  openedAt: string;
  closesAt: string;
}

export const publishedTestsApi = {
  getInfo: (id: string) =>
    api.get<PublishedTestInfoDto>(`/api/published-tests/${id}/info`).then((r) => r.data),
  listForTeacher: () =>
    api.get<PublishedTestListItemDto[]>(`/api/teacher/published-tests`).then((r) => r.data),
};
