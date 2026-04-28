import { api } from './client';
import type {
  ClassroomCourseDto,
  PublishTestTemplateRequest,
  TestTemplateAssignmentDto,
  TestTemplateDto,
  TestTemplateInput,
  TestTemplateSummaryDto,
} from '../types/testTemplates';

export const testTemplatesApi = {
  list: () => api.get<TestTemplateSummaryDto[]>('/api/test-templates').then((r) => r.data),
  get: (id: string) => api.get<TestTemplateDto>(`/api/test-templates/${id}`).then((r) => r.data),
  create: (input: TestTemplateInput) =>
    api.post<TestTemplateDto>('/api/test-templates', input).then((r) => r.data),
  update: (id: string, input: TestTemplateInput) =>
    api.put<TestTemplateDto>(`/api/test-templates/${id}`, input).then((r) => r.data),
  remove: (id: string) => api.delete(`/api/test-templates/${id}`).then(() => undefined),
  publish: (id: string, body: PublishTestTemplateRequest) =>
    api.post<TestTemplateAssignmentDto[]>(`/api/test-templates/${id}/publish`, body).then((r) => r.data),
};

export const classroomApi = {
  courses: () =>
    api.get<ClassroomCourseDto[]>('/api/classroom/courses').then((r) => r.data),
};
