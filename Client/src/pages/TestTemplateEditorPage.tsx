import { useEffect, useState } from 'react';
import { Alert, Button, Card, Col, Container, Form, InputGroup, Row, Spinner, Stack } from 'react-bootstrap';
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form';
import type { Control, FieldErrors, UseFormRegister, UseFormSetValue } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import axios from 'axios';
import { testTemplatesApi } from '../api/testTemplates';
import type { QuestionType, TestTemplateDto, TestTemplateInput } from '../types/testTemplates';
import { PlusIcon, TrashIcon } from '../components/icons';

type FormValues = {
  name: string;
  description: string;
  hasTimeLimit: boolean;
  timeLimitMinutes: number | '';
  questions: {
    questionId?: string;
    text: string;
    mark: number | '';
    type: QuestionType;
    answers: { text: string; isCorrect: boolean }[];
  }[];
};

const QUESTION_TYPE_OPTIONS: { value: QuestionType; label: string }[] = [
  { value: 'SingleAnswer', label: 'Single answer' },
  { value: 'MultipleAnswers', label: 'Multiple answers' },
  { value: 'OpenAnswer', label: 'Open answer (text)' },
  { value: 'Code', label: 'Code' },
  { value: 'Diagram', label: 'Diagram' },
];

const hasOptions = (t: QuestionType) => t === 'SingleAnswer' || t === 'MultipleAnswers';

const blankAnswer = () => ({ text: '', isCorrect: false });
const blankQuestion = () => ({
  text: '',
  mark: 1 as number | '',
  type: 'SingleAnswer' as QuestionType,
  answers: [blankAnswer(), blankAnswer()],
});

function dtoToForm(template: TestTemplateDto | null): FormValues {
  if (!template) {
    return {
      name: '',
      description: '',
      hasTimeLimit: false,
      timeLimitMinutes: '',
      questions: [blankQuestion()],
    };
  }
  return {
    name: template.name,
    description: template.description ?? '',
    hasTimeLimit: template.timeLimitMinutes != null,
    timeLimitMinutes: template.timeLimitMinutes ?? '',
    questions: template.questions.map((q) => ({
      questionId: q.id,
      text: q.text,
      mark: q.mark,
      type: q.type,
      answers: hasOptions(q.type)
        ? q.answers.map((a) => ({ text: a.text, isCorrect: a.isCorrect }))
        : [],
    })),
  };
}

function formToInput(values: FormValues): TestTemplateInput {
  return {
    name: values.name.trim(),
    description: values.description.trim() ? values.description.trim() : null,
    timeLimitMinutes:
      values.hasTimeLimit && values.timeLimitMinutes !== ''
        ? Number(values.timeLimitMinutes)
        : null,
    questions: values.questions.map((q, i) => ({
      id: q.questionId,
      text: q.text.trim(),
      order: i,
      mark: typeof q.mark === 'number' ? q.mark : 1,
      type: q.type,
      answers: hasOptions(q.type)
        ? q.answers.map((a) => ({ text: a.text.trim(), isCorrect: a.isCorrect }))
        : [],
    })),
  };
}

export function TestTemplateEditorPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();

  const [loading, setLoading] = useState(isEdit);
  const [loadError, setLoadError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    control,
    watch,
    reset,
    setError,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ defaultValues: dtoToForm(null) });

  const questions = useFieldArray({ control, name: 'questions' });
  const hasTimeLimit = watch('hasTimeLimit');

  useEffect(() => {
    if (!isEdit) return;
    let cancelled = false;
    setLoading(true);
    testTemplatesApi
      .get(id!)
      .then((dto) => {
        if (cancelled) return;
        reset(dtoToForm(dto));
      })
      .catch((e) => {
        if (cancelled) return;
        let msg = 'Failed to load test.';
        if (axios.isAxiosError(e)) msg = e.response?.data?.error ?? msg;
        setLoadError(msg);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [id, isEdit, reset]);

  const onSubmit = async (values: FormValues) => {
    const payload = formToInput(values);
    try {
      if (isEdit && id) {
        await testTemplatesApi.update(id, payload);
      } else {
        await testTemplatesApi.create(payload);
      }
      navigate('/test-templates');
    } catch (e) {
      let msg = 'Failed to save test.';
      if (axios.isAxiosError(e)) {
        const data = e.response?.data;
        msg = data?.error ?? data?.errors?.[0] ?? msg;
      }
      setError('root', { message: msg });
    }
  };

  if (loading) {
    return (
      <Container className="py-4">
        <div className="d-flex align-items-center gap-2 text-muted">
          <Spinner animation="border" size="sm" /> Loading…
        </div>
      </Container>
    );
  }

  if (loadError) {
    return (
      <Container className="py-4">
        <Alert variant="danger">{loadError}</Alert>
        <Button variant="secondary" onClick={() => navigate('/test-templates')}>
          Back to tests
        </Button>
      </Container>
    );
  }

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h3 mb-0">{isEdit ? 'Edit test' : 'Create test'}</h1>
        <Button variant="link" onClick={() => navigate('/test-templates')}>
          ← Back to tests
        </Button>
      </div>

      <Form onSubmit={handleSubmit(onSubmit)}>
        {errors.root && <Alert variant="danger">{errors.root.message}</Alert>}

        <Card className="mb-3">
          <Card.Body>
            <Form.Group className="mb-3">
              <Form.Label>Name</Form.Label>
              <Form.Control
                isInvalid={!!errors.name}
                {...register('name', { required: 'Name is required', maxLength: 200 })}
              />
              <Form.Control.Feedback type="invalid">{errors.name?.message}</Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Description</Form.Label>
              <Form.Control
                as="textarea"
                rows={2}
                isInvalid={!!errors.description}
                {...register('description', { maxLength: 2000 })}
              />
            </Form.Group>

            <Row className="align-items-end">
              <Col xs="auto">
                <Form.Check
                  type="checkbox"
                  id="hasTimeLimit"
                  label="Limited time to complete"
                  {...register('hasTimeLimit')}
                />
              </Col>
              {hasTimeLimit && (
                <Col xs={12} sm={4}>
                  <Form.Label>Time limit (minutes)</Form.Label>
                  <Form.Control
                    type="number"
                    min={1}
                    max={600}
                    isInvalid={!!errors.timeLimitMinutes}
                    {...register('timeLimitMinutes', {
                      valueAsNumber: true,
                      validate: (v) =>
                        !hasTimeLimit ||
                        (typeof v === 'number' && v >= 1 && v <= 600) ||
                        'Enter 1–600 minutes',
                    })}
                  />
                  <Form.Control.Feedback type="invalid">
                    {errors.timeLimitMinutes?.message}
                  </Form.Control.Feedback>
                </Col>
              )}
            </Row>
          </Card.Body>
        </Card>

        <div className="mb-2">
          <h5 className="mb-0">Questions</h5>
        </div>

        <Stack gap={3} className="mb-2">
          {questions.fields.map((q, qi) => (
            <Card key={q.id}>
              <Card.Body>
                <div className="d-flex justify-content-between align-items-start mb-2">
                  <strong>Question {qi + 1}</strong>
                  <Button
                    size="sm"
                    variant="outline-danger"
                    disabled={questions.fields.length === 1}
                    onClick={() => questions.remove(qi)}
                    title="Remove question"
                  >
                    <TrashIcon />
                  </Button>
                </div>
                <Row className="g-2 mb-3">
                  <Col xs={12} md={6}>
                    <Form.Control
                      placeholder="Question text"
                      isInvalid={!!errors.questions?.[qi]?.text}
                      {...register(`questions.${qi}.text`, { required: 'Required' })}
                    />
                    <Form.Control.Feedback type="invalid">
                      {errors.questions?.[qi]?.text?.message}
                    </Form.Control.Feedback>
                  </Col>
                  <Col xs={6} md={2}>
                    <InputGroup hasValidation>
                      <InputGroup.Text>Mark</InputGroup.Text>
                      <Form.Control
                        type="number"
                        min={1}
                        max={1000}
                        isInvalid={!!errors.questions?.[qi]?.mark}
                        {...register(`questions.${qi}.mark`, {
                          valueAsNumber: true,
                          required: 'Required',
                          validate: (v) =>
                            (typeof v === 'number' && Number.isFinite(v) && v >= 1 && v <= 1000) ||
                            'Enter 1–1000',
                        })}
                      />
                      <Form.Control.Feedback type="invalid">
                        {errors.questions?.[qi]?.mark?.message}
                      </Form.Control.Feedback>
                    </InputGroup>
                  </Col>
                  <Col xs={6} md={4}>
                    <Controller
                      control={control}
                      name={`questions.${qi}.type`}
                      render={({ field }) => (
                        <Form.Select
                          value={field.value}
                          onChange={(e) => {
                            const next = e.target.value as QuestionType;
                            field.onChange(next);
                            if (hasOptions(next)) {
                              const cur = (watch(`questions.${qi}.answers`) ?? []) as {
                                text: string;
                                isCorrect: boolean;
                              }[];
                              if (cur.length < 2) {
                                setValue(`questions.${qi}.answers`, [
                                  ...cur,
                                  ...Array.from({ length: 2 - cur.length }, () => blankAnswer()),
                                ]);
                              }
                              if (next === 'SingleAnswer') {
                                const correctIdx = cur.findIndex((a) => a.isCorrect);
                                cur.forEach((_, i) =>
                                  setValue(
                                    `questions.${qi}.answers.${i}.isCorrect`,
                                    i === correctIdx,
                                  ),
                                );
                              }
                            } else {
                              setValue(`questions.${qi}.answers`, []);
                            }
                          }}
                        >
                          {QUESTION_TYPE_OPTIONS.map((opt) => (
                            <option key={opt.value} value={opt.value}>
                              {opt.label}
                            </option>
                          ))}
                        </Form.Select>
                      )}
                    />
                  </Col>
                </Row>
                <AnswersField
                  control={control}
                  register={register}
                  setValue={setValue}
                  qi={qi}
                  errors={errors}
                />
              </Card.Body>
            </Card>
          ))}
        </Stack>

        <div className="d-flex flex-column align-items-start mb-4">
          {errors.questions?.root?.message && (
            <div className="text-danger small mb-2">{errors.questions.root.message}</div>
          )}
          <Button
            size="sm"
            variant="outline-primary"
            onClick={() => questions.append(blankQuestion())}
          >
            <PlusIcon /> Add question
          </Button>
        </div>

        <div className="d-flex gap-2 justify-content-end">
          <Button variant="secondary" onClick={() => navigate('/test-templates')} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create test'}
          </Button>
        </div>
      </Form>
    </Container>
  );
}

interface AnswersProps {
  qi: number;
  control: Control<FormValues>;
  register: UseFormRegister<FormValues>;
  setValue: UseFormSetValue<FormValues>;
  errors: FieldErrors<FormValues>;
}

function AnswersField({ qi, control, register, setValue, errors }: AnswersProps) {
  const answers = useFieldArray({ control, name: `questions.${qi}.answers` });
  const type = useWatch({ control, name: `questions.${qi}.type` });

  if (!hasOptions(type)) {
    return (
      <div className="text-muted small fst-italic">
        Answer collection for this question type isn’t implemented yet.
      </div>
    );
  }

  const isSingle = type === 'SingleAnswer';
  return (
    <>
      <Form.Label className="small text-muted">
        {isSingle ? 'Answers (mark the correct one)' : 'Answers (mark correct ones)'}
      </Form.Label>
      <Stack gap={2}>
        {answers.fields.map((a, ai) => (
          <div key={a.id} className="d-flex align-items-center gap-2">
            <Controller
              control={control}
              name={`questions.${qi}.answers.${ai}.isCorrect`}
              render={({ field }) => (
                <Form.Check
                  type={isSingle ? 'radio' : 'checkbox'}
                  name={isSingle ? `questions.${qi}.correct` : undefined}
                  checked={!!field.value}
                  onChange={() => {
                    if (isSingle) {
                      answers.fields.forEach((_, i) =>
                        setValue(`questions.${qi}.answers.${i}.isCorrect`, i === ai),
                      );
                    } else {
                      field.onChange(!field.value);
                    }
                  }}
                  title="Correct?"
                />
              )}
            />
            <Form.Control
              placeholder={`Answer ${ai + 1}`}
              isInvalid={!!errors.questions?.[qi]?.answers?.[ai]?.text}
              {...register(`questions.${qi}.answers.${ai}.text`, { required: 'Required' })}
            />
            <Button
              size="sm"
              variant="outline-secondary"
              disabled={answers.fields.length <= 2}
              onClick={() => answers.remove(ai)}
              title="Remove answer"
            >
              <TrashIcon />
            </Button>
          </div>
        ))}
      </Stack>
      <Button
        size="sm"
        variant="link"
        className="mt-1 p-0"
        onClick={() => answers.append({ text: '', isCorrect: false })}
      >
        <PlusIcon /> Add answer
      </Button>
    </>
  );
}
