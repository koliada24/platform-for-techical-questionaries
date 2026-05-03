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
  getAttemptDetail: (attemptId: string) =>
    api
      .get<AttemptDetailForTeacherDto>(`/api/teacher/published-tests/attempts/${attemptId}`)
      .then((r) => r.data),
  setManualMarks: (attemptId: string, marks: SetManualMarkInput[]) =>
    api
      .put<AttemptDetailForTeacherDto>(
        `/api/teacher/published-tests/attempts/${attemptId}/marks`,
        { marks },
      )
      .then((r) => r.data),
  sendMark: (attemptId: string) =>
    api
      .post<{ mark: number; maxMark: number }>(
        `/api/teacher/published-tests/attempts/${attemptId}/send-mark`,
      )
      .then((r) => r.data),
};

export type QuestionType =
  | 'SingleAnswer'
  | 'MultipleAnswers'
  | 'OpenAnswer'
  | 'Code'
  | 'Diagram';

export interface AttemptAnswerOptionDto {
  order: number;
  text: string;
  isCorrect: boolean;
}

export interface AttemptQuestionForTeacherDto {
  publishedQuestionId: string;
  text: string;
  order: number;
  maxMark: number;
  type: QuestionType;
  codeLanguage: string | null;
  options: AttemptAnswerOptionDto[];
  selectedOptionOrder: number | null;
  selectedOptionOrders: number[] | null;
  answerText: string | null;
  mark: number | null;
  isAutoEvaluated: boolean;
}

export interface AttemptDetailForTeacherDto {
  attemptId: string;
  testTemplateId: string;
  closesAt: string;
  testName: string;
  studentId: string;
  studentName: string | null;
  studentEmail: string | null;
  studentPictureUrl: string | null;
  startedAt: string;
  submittedAt: string;
  durationSeconds: number;
  maxMark: number;
  totalMark: number;
  isFullyEvaluated: boolean;
  markSent: boolean;
  questions: AttemptQuestionForTeacherDto[];
}

export interface SetManualMarkInput {
  publishedQuestionId: string;
  mark: number | null;
}
