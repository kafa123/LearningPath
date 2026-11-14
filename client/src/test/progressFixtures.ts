import type {
  AssessmentHistoryItem,
  ExaminationSessionDetail,
  ExaminationSessionSummary,
  ProgressOverview,
} from '@/shared/api/progress'
import { twoSumDetail } from './challengeFixtures'

export const progressOverviewFixture: ProgressOverview = {
  summary: {
    total: 4,
    notStarted: 1,
    learning: 1,
    practicing: 0,
    assessmentRequired: 1,
    mastered: 1,
  },
  domains: [
    {
      slug: 'programming-fundamentals',
      name: 'Programming Fundamentals',
      description: 'Algorithms, data structures, and the core techniques behind reliable programs.',
      sortOrder: 1,
      modules: [
        {
          slug: 'algorithm-foundations',
          title: 'Algorithm Foundations',
          level: 'Beginner',
          sortOrder: 1,
          competencies: [
            {
              slug: 'algorithm-decomposition',
              name: 'Problem decomposition',
              description: 'Break a requirement into inputs, outputs, and edge cases.',
              state: 'Mastered',
              nextAction: 'Keep the competency current with another challenge.',
              evidence: [
                {
                  challengeSlug: 'two-sum',
                  challengeTitle: 'Two Sum',
                  purpose: 'Submit',
                  outcome: 'Accepted',
                  score: 100,
                  passedTests: 4,
                  totalTests: 4,
                  isExamination: true,
                  createdAtUtc: '2026-11-11T10:00:00Z',
                },
              ],
            },
            {
              slug: 'correctness-reasoning',
              name: 'Correctness reasoning',
              description: 'Justify correctness with examples and boundary cases.',
              state: 'AssessmentRequired',
              nextAction: 'Take an examination for a mapped challenge.',
              evidence: [
                {
                  challengeSlug: 'two-sum',
                  challengeTitle: 'Two Sum',
                  purpose: 'Submit',
                  outcome: 'Accepted',
                  score: 100,
                  passedTests: 4,
                  totalTests: 4,
                  isExamination: false,
                  createdAtUtc: '2026-11-11T09:00:00Z',
                },
              ],
            },
          ],
        },
        {
          slug: 'complexity-analysis',
          title: 'Complexity Analysis',
          level: 'Intermediate',
          sortOrder: 2,
          competencies: [
            {
              slug: 'asymptotic-analysis',
              name: 'Asymptotic analysis',
              description: 'Estimate time and space growth using Big-O notation.',
              state: 'Learning',
              nextAction: 'Submit a mapped challenge to run the hidden assessment tests.',
              evidence: [
                {
                  challengeSlug: 'two-sum',
                  challengeTitle: 'Two Sum',
                  purpose: 'Run',
                  outcome: 'Accepted',
                  score: 100,
                  passedTests: 2,
                  totalTests: 2,
                  isExamination: false,
                  createdAtUtc: '2026-11-11T08:00:00Z',
                },
              ],
            },
            {
              slug: 'structure-selection',
              name: 'Structure selection',
              description: 'Choose a data structure that matches the access pattern.',
              state: 'NotStarted',
              nextAction: 'Open a mapped challenge and run its public tests.',
              evidence: [],
            },
          ],
        },
      ],
    },
  ],
}

export const assessmentHistoryFixture: AssessmentHistoryItem[] = [
  {
    submissionId: 'sub-exam',
    challengeSlug: 'two-sum',
    challengeTitle: 'Two Sum',
    languageId: 'csharp',
    purpose: 'Submit',
    outcome: 'Accepted',
    isAccepted: true,
    score: 100,
    passedTests: 4,
    totalTests: 4,
    publicPassed: 2,
    publicTotal: 2,
    hiddenPassed: 2,
    hiddenTotal: 2,
    isExamination: true,
    examinationId: 'exam-1',
    examinationVariant: 'Standard examination',
    createdAtUtc: '2026-11-11T10:00:00Z',
  },
  {
    submissionId: 'sub-practice',
    challengeSlug: 'balanced-brackets',
    challengeTitle: 'Balanced Brackets',
    languageId: 'csharp',
    purpose: 'Submit',
    outcome: 'WrongAnswer',
    isAccepted: false,
    score: 50,
    passedTests: 2,
    totalTests: 4,
    publicPassed: 2,
    publicTotal: 2,
    hiddenPassed: 0,
    hiddenTotal: 2,
    isExamination: false,
    examinationId: null,
    examinationVariant: null,
    createdAtUtc: '2026-11-11T09:00:00Z',
  },
]

export const examinationSummaryFixture: ExaminationSessionSummary = {
  id: 'exam-1',
  challengeSlug: 'two-sum',
  challengeTitle: 'Two Sum',
  variantId: 'standard',
  variantLabel: 'Standard examination',
  timeLimitSeconds: 600,
  status: 'InProgress',
  isActive: true,
  remainingSeconds: 540,
  startedAtUtc: '2026-11-11T10:00:00Z',
  expiresAtUtc: '2026-11-11T10:10:00Z',
  completedAtUtc: null,
  attemptCount: 1,
}

export const examinationDetailFixture: ExaminationSessionDetail = {
  id: 'exam-1',
  challengeSlug: 'two-sum',
  challengeTitle: 'Two Sum',
  variantId: 'standard',
  variantLabel: 'Standard examination',
  timeLimitSeconds: 600,
  status: 'InProgress',
  isActive: true,
  remainingSeconds: 540,
  startedAtUtc: '2026-11-11T10:00:00Z',
  expiresAtUtc: null,
  completedAtUtc: null,
  challenge: {
    ...twoSumDetail,
    hintsMarkdown: null,
  },
}

export const completedExaminationFixture: ExaminationSessionDetail = {
  ...examinationDetailFixture,
  status: 'Completed',
  isActive: false,
  remainingSeconds: null,
  completedAtUtc: '2026-11-11T10:05:00Z',
}

export const expiredExaminationFixture: ExaminationSessionDetail = {
  ...examinationDetailFixture,
  status: 'Expired',
  isActive: false,
  remainingSeconds: 0,
  expiresAtUtc: '2026-11-11T10:10:00Z',
}
