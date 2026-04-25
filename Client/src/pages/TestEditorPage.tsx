import { useEffect, useState } from 'react';
import { Alert, Button, Card, Col, Container, Form, Row, Spinner, Stack } from 'react-bootstrap';
import { Controller, useFieldArray, useForm } from 'react-hook-form';
import type { Control, FieldErrors, UseFormRegister } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import axios from 'axios';
import { testsApi } from '../api/tests';
import type { TestDto, TestInput } from '../types/tests';
import { PlusIcon, TrashIcon } from '../components/icons';

type FormValues = {
  name: string;
  description: string;
  hasTimeLimit: boolean;
  timeLimitMinutes: number | '';
  questions: {
    questionId?: string;
    text: string;
    answers: { text: string; isCorrect: boolean }[];
  }[];
};

const blankAnswer = () => ({ text: '', isCorrect: false });
const blankQuestion = () => ({ text: '', answers: [blankAnswer(), blankAnswer()] });

function dtoToForm(test: TestDto | null): FormValues {
  if (!test) {
    return {
      name: '',
      description: '',
      hasTimeLimit: false,
      timeLimitMinutes: '',
      questions: [blankQuestion()],
    };
  }
  return {
    name: test.name,
    description: test.description ?? '',
    hasTimeLimit: test.timeLimitMinutes != null,
    timeLimitMinutes: test.timeLimitMinutes ?? '',
    questions: test.questions.map((q) => ({
      questionId: q.id,
      text: q.text,
      answers: q.answers.map((a) => ({ text: a.text, isCorrect: a.isCorrect })),
    })),
  };
}

function formToInput(values: FormValues): TestInput {
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
      answers: q.answers.map((a) => ({ text: a.text.trim(), isCorrect: a.isCorrect })),
    })),
  };
}

export function TestEditorPage() {
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
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ defaultValues: dtoToForm(null) });

  const questions = useFieldArray({ control, name: 'questions' });
  const hasTimeLimit = watch('hasTimeLimit');

  useEffect(() => {
    if (!isEdit) return;
    let cancelled = false;
    setLoading(true);
    testsApi
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
        await testsApi.update(id, payload);
      } else {
        await testsApi.create(payload);
      }
      navigate('/tests');
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
        <Button variant="secondary" onClick={() => navigate('/tests')}>
          Back to tests
        </Button>
      </Container>
    );
  }

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h3 mb-0">{isEdit ? 'Edit test' : 'Create test'}</h1>
        <Button variant="link" onClick={() => navigate('/tests')}>
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

        <div className="d-flex justify-content-between align-items-center mb-2">
          <h5 className="mb-0">Questions</h5>
          <Button
            size="sm"
            variant="outline-primary"
            onClick={() => questions.append(blankQuestion())}
          >
            <PlusIcon /> Add question
          </Button>
        </div>

        <Stack gap={3} className="mb-4">
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
                <Form.Group className="mb-3">
                  <Form.Control
                    placeholder="Question text"
                    isInvalid={!!errors.questions?.[qi]?.text}
                    {...register(`questions.${qi}.text`, { required: 'Required' })}
                  />
                </Form.Group>
                <AnswersField control={control} register={register} qi={qi} errors={errors} />
              </Card.Body>
            </Card>
          ))}
        </Stack>

        <div className="d-flex gap-2 justify-content-end">
          <Button variant="secondary" onClick={() => navigate('/tests')} disabled={isSubmitting}>
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
  errors: FieldErrors<FormValues>;
}

function AnswersField({ qi, control, register, errors }: AnswersProps) {
  const answers = useFieldArray({ control, name: `questions.${qi}.answers` });
  return (
    <>
      <Form.Label className="small text-muted">Answers (mark correct ones)</Form.Label>
      <Stack gap={2}>
        {answers.fields.map((a, ai) => (
          <div key={a.id} className="d-flex align-items-center gap-2">
            <Controller
              control={control}
              name={`questions.${qi}.answers.${ai}.isCorrect`}
              render={({ field }) => (
                <Form.Check
                  type="checkbox"
                  checked={!!field.value}
                  onChange={(e) => field.onChange(e.target.checked)}
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
