export type QuestionType =
  | 'SingleAnswer'
  | 'MultipleAnswers'
  | 'OpenAnswer'
  | 'Code'
  | 'Diagram';

export interface AnswerDto {
  text: string;
  isCorrect: boolean;
  order: number;
}

export interface QuestionDto {
  id: string;
  text: string;
  order: number;
  type: QuestionType;
  answers: AnswerDto[];
}

export interface TestTemplateDto {
  id: string;
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  createdAt: string;
  updatedAt: string;
  questions: QuestionDto[];
}

export interface TestTemplateSummaryDto {
  id: string;
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  questionCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface AnswerInput {
  text: string;
  isCorrect: boolean;
}

export interface QuestionInput {
  id?: string;
  text: string;
  order: number;
  type: QuestionType;
  answers: AnswerInput[];
}

export interface TestTemplateInput {
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  questions: QuestionInput[];
}

export interface ClassroomCourseDto {
  id: string;
  name: string;
  section: string | null;
  description: string | null;
}

export interface PublishTestTemplateRequest {
  courseIds: string[];
  closesAt: string; // ISO
}

/** A published test snapshot (one per Google Classroom course). */
export interface TestSummaryDto {
  id: string;
  name: string;
  description: string | null;
  timeLimitMinutes: number | null;
  googleCourseId: string;
  googleCourseName: string;
  closesAt: string;
  createdAt: string;
}
