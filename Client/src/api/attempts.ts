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

export interface SavedAnswerDto {
  type: QuestionType;
  selectedOptionOrder: number | null;
  selectedOptionOrders: number[] | null;
  text: string | null;
}

export interface AttemptQuestionForStudentDto {
  id: string;
  text: string;
  order: number;
  type: QuestionType;
  codeLanguage: string | null;
  options: AnswerOptionForStudentDto[];
  savedAnswer: SavedAnswerDto | null;
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
  saveSingleAnswer: (attemptId: string, questionId: string, selectedOptionOrder: number | null) =>
    api.put<void>(`/api/attempts/${attemptId}/questions/${questionId}/single-answer`, {
      selectedOptionOrder,
    }),
  saveMultipleAnswers: (attemptId: string, questionId: string, selectedOptionOrders: number[]) =>
    api.put<void>(`/api/attempts/${attemptId}/questions/${questionId}/multiple-answers`, {
      selectedOptionOrders,
    }),
  saveTextAnswer: (attemptId: string, questionId: string, text: string) =>
    api.put<void>(`/api/attempts/${attemptId}/questions/${questionId}/text-answer`, { text }),
  saveCodeAnswer: (attemptId: string, questionId: string, text: string) =>
    api.put<void>(`/api/attempts/${attemptId}/questions/${questionId}/code-answer`, { text }),
  saveDiagramAnswer: (attemptId: string, questionId: string, text: string) =>
    api.put<void>(`/api/attempts/${attemptId}/questions/${questionId}/diagram-answer`, { text }),
  clearAnswer: (attemptId: string, questionId: string) =>
    api.delete<void>(`/api/attempts/${attemptId}/questions/${questionId}/answer`),
  submit: (attemptId: string) =>
    api
      .post<{ submittedAttemptId: string }>(`/api/attempts/${attemptId}/submit`)
      .then((r) => r.data),
};
