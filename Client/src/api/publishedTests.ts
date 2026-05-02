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

export interface SubmittedAttemptSummaryDto {
  id: string;
  studentId: string;
  studentName: string | null;
  studentEmail: string | null;
  studentPictureUrl: string | null;
  startedAt: string;
  submittedAt: string;
  durationSeconds: number;
  evaluatedMark: number;
  isEvaluated: boolean;
}

export interface PublishedTestDetailDto {
  testTemplateId: string;
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  questionCount: number;
  maxMark: number;
  courseCount: number;
  openedAt: string;
  closesAt: string;
  submittedAttempts: SubmittedAttemptSummaryDto[];
}

export const publishedTestsApi = {
  getInfo: (id: string) =>
    api.get<PublishedTestInfoDto>(`/api/published-tests/${id}/info`).then((r) => r.data),
  listForTeacher: () =>
    api.get<PublishedTestListItemDto[]>(`/api/teacher/published-tests`).then((r) => r.data),
  getTeacherDetail: (testTemplateId: string, closesAt: string) =>
    api
      .get<PublishedTestDetailDto>(`/api/teacher/published-tests/details`, {
        params: { testTemplateId, closesAt },
      })
      .then((r) => r.data),
};
