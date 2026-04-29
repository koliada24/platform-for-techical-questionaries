import { api } from './client';
import type { QuestionType } from '../types/testTemplates';

export interface AttemptInProgressDto {
  id: string;
  publishedTestId: string;
  startedAt: string;
}

export interface AnswerOptionForStudentDto {
  order: number;
  text: string;
}

export interface AttemptQuestionForStudentDto {
  id: string;
  text: string;
  order: number;
  type: QuestionType;
  options: AnswerOptionForStudentDto[];
}

export interface AttemptForStudentDto {
  id: string;
  publishedTestId: string;
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  startedAt: string;
  closesAt: string;
  questions: AttemptQuestionForStudentDto[];
}

export const attemptsApi = {
  start: (publishedTestId: string) =>
    api
      .post<AttemptInProgressDto>(`/api/published-tests/${publishedTestId}/attempts`)
      .then((r) => r.data),
  get: (attemptId: string) =>
    api.get<AttemptForStudentDto>(`/api/attempts/${attemptId}`).then((r) => r.data),
};
