import { api } from './client';
import type {
  ClassroomCourseDto,
  PublishTestRequest,
  TestAssignmentDto,
  TestDto,
  TestInput,
  TestSummaryDto,
} from '../types/tests';

export const testsApi = {
  list: () => api.get<TestSummaryDto[]>('/api/tests').then((r) => r.data),
  get: (id: string) => api.get<TestDto>(`/api/tests/${id}`).then((r) => r.data),
  create: (input: TestInput) => api.post<TestDto>('/api/tests', input).then((r) => r.data),
  update: (id: string, input: TestInput) =>
    api.put<TestDto>(`/api/tests/${id}`, input).then((r) => r.data),
  remove: (id: string) => api.delete(`/api/tests/${id}`).then(() => undefined),
  publish: (id: string, body: PublishTestRequest) =>
    api.post<TestAssignmentDto[]>(`/api/tests/${id}/publish`, body).then((r) => r.data),
};

export const classroomApi = {
  courses: () =>
    api.get<ClassroomCourseDto[]>('/api/classroom/courses').then((r) => r.data),
};
