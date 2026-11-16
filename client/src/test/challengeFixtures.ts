import type { ChallengeDetail, ChallengeSummary } from '@/shared/api/challenges'

export const twoSumSummary: ChallengeSummary = {
  slug: 'two-sum',
  title: 'Two Sum',
  summary: 'Return the indices of the two numbers that add up to a target value.',
  domainSlug: 'programming-fundamentals',
  topic: 'Arrays and hash maps',
  difficulty: 'Beginner',
  type: 'Coding',
  estimatedMinutes: 30,
  languages: [
    { id: 'csharp', name: 'C#', canExecute: true },
    { id: 'python', name: 'Python', canExecute: true },
  ],
  prerequisiteSlugs: ['algorithm-foundations', 'data-structures'],
  competencySlugs: [
    'algorithm-decomposition',
    'correctness-reasoning',
    'structure-selection',
    'asymptotic-analysis',
  ],
}

export const apiValidationSummary: ChallengeSummary = {
  slug: 'api-validation',
  title: 'Add Request Validation to an Endpoint',
  summary: 'Harden a registration endpoint with explicit validation.',
  domainSlug: 'backend-engineering',
  topic: 'HTTP API design',
  difficulty: 'Intermediate',
  type: 'API',
  estimatedMinutes: 45,
  languages: [{ id: 'csharp', name: 'C#', canExecute: false }],
  prerequisiteSlugs: ['http-api-design'],
  competencySlugs: [
    'input-validation',
    'safe-error-responses',
    'resource-modeling',
    'http-semantics',
  ],
}

export const orderRevenueSummary: ChallengeSummary = {
  slug: 'order-revenue',
  title: 'Completed revenue per customer',
  summary:
    'Aggregate completed order revenue for every customer, including customers with no orders.',
  domainSlug: 'database-engineering',
  topic: 'Joins, grouping, and NULL semantics',
  difficulty: 'Intermediate',
  type: 'SQL',
  estimatedMinutes: 30,
  languages: [{ id: 'sql', name: 'SQL', canExecute: true }],
  prerequisiteSlugs: ['sql-querying'],
  competencySlugs: ['sql-composition', 'null-semantics'],
}

export const orderRevenueDetail: ChallengeDetail = {
  slug: 'order-revenue',
  title: 'Completed revenue per customer',
  summary:
    'Aggregate completed order revenue for every customer, including customers with no orders.',
  domainSlug: 'database-engineering',
  topic: 'Joins, grouping, and NULL semantics',
  difficulty: 'Intermediate',
  type: 'SQL',
  estimatedMinutes: 30,
  objectives: [
    'Use a LEFT JOIN to keep customers that have no matching orders.',
    'Aggregate currency values and handle empty groups with COALESCE.',
  ],
  prerequisiteSlugs: ['sql-querying'],
  competencySlugs: ['sql-composition', 'null-semantics'],
  constraints: [
    'Return exactly the columns name, completed_orders, and revenue in that order.',
  ],
  examples: [
    {
      input: 'A customer with two completed orders worth 12000 and 13000 cents.',
      output: "name = 'Alice', completed_orders = 2, revenue = 250.0",
      explanation: 'Completed revenue is summed and divided by 100.',
    },
  ],
  languages: [
    {
      id: 'sql',
      name: 'SQL',
      editorLanguage: 'sql',
      canExecute: true,
      starterFiles: [
        {
          path: 'query.sql',
          content: 'SELECT c.name, 0 AS completed_orders, 0.0 AS revenue FROM customers AS c;\n',
        },
      ],
    },
  ],
  publicTests: [
    {
      name: 'Standard customer set',
      description: 'Every seeded customer is returned with completed revenue.',
    },
    {
      name: 'Single customer',
      description: 'One customer with one completed order produces one row.',
    },
  ],
  evaluation: {
    kind: 'AutomatedTests',
    timeLimitSeconds: 10,
    memoryLimitMb: 256,
  },
  examination: {
    variants: [
      { id: 'standard', label: 'Standard examination', timeLimitSeconds: 600 },
    ],
  },
  statementMarkdown: [
    '# Completed revenue per customer',
    '',
    'Write one query that returns every customer with completed revenue.',
  ].join('\n'),
  hintsMarkdown: '# Hints\n\nUse a LEFT JOIN.',
}

export const refactorDuplicationDetail: ChallengeDetail = {
  slug: 'refactor-duplication',
  title: 'Refactor Duplicated Discount Rules',
  summary: 'Remove duplicated pricing rules without changing observable behavior.',
  domainSlug: 'software-quality',
  topic: 'Refactoring',
  difficulty: 'Intermediate',
  type: 'Refactoring',
  estimatedMinutes: 40,
  objectives: [
    'Identify duplicated rules as a single source of truth.',
    'Extract a helper while preserving the public behavior.',
  ],
  prerequisiteSlugs: ['testing-foundations'],
  competencySlugs: ['test-design', 'responsibility-boundaries'],
  constraints: [
    'Behavior must not change for any tier or price.',
    'Keep both public function signatures unchanged.',
  ],
  examples: [
    {
      input: 'ApplyDiscount(120, "standard")',
      output: '108',
      explanation: 'Standard prices at or above 100 receive a 10% discount.',
    },
  ],
  languages: [
    {
      id: 'csharp',
      name: 'C#',
      editorLanguage: 'csharp',
      canExecute: true,
      starterFiles: [
        {
          path: 'Solution.cs',
          content: 'namespace Challenge;\n\npublic static class Solution\n{\n}\n',
        },
      ],
    },
  ],
  publicTests: [
    { name: 'Standard discount', description: 'Standard tier receives its discount.' },
  ],
  evaluation: {
    kind: 'Hybrid',
    timeLimitSeconds: 5,
    memoryLimitMb: 256,
  },
  rubric: {
    criteria: [
      {
        id: 'behavior-preservation',
        title: 'Behavior preservation',
        description: 'Public behavior stays identical for every tier and price.',
        maxPoints: 10,
        weight: 3,
      },
      {
        id: 'single-source-of-truth',
        title: 'Single source of truth',
        description: 'The duplicated discount rules collapse into one implementation.',
        maxPoints: 10,
        weight: 3,
      },
      {
        id: 'clarity',
        title: 'Clarity',
        description: 'Names and structure make the rule easy to read.',
        maxPoints: 10,
        weight: 2,
      },
      {
        id: 'test-confidence',
        title: 'Test confidence',
        description: 'The refactoring is backed by the existing tests.',
        maxPoints: 10,
        weight: 2,
      },
    ],
  },
  examination: {
    variants: [{ id: 'standard', label: 'Standard examination', timeLimitSeconds: 600 }],
  },
  statementMarkdown: '# Refactor duplicated discount rules',
  hintsMarkdown: '# Hints\n\nLook for the repeated condition.',
}

export const twoSumDetail: ChallengeDetail = {
  slug: 'two-sum',
  title: 'Two Sum',
  summary: 'Return the indices of the two numbers that add up to a target value.',
  domainSlug: 'programming-fundamentals',
  topic: 'Arrays and hash maps',
  difficulty: 'Beginner',
  type: 'Coding',
  estimatedMinutes: 30,
  objectives: [
    'Translate a problem statement into an explicit function contract.',
    'Use a hash map to trade memory for a faster lookup.',
  ],
  prerequisiteSlugs: ['algorithm-foundations', 'data-structures'],
  competencySlugs: [
    'algorithm-decomposition',
    'correctness-reasoning',
    'structure-selection',
    'asymptotic-analysis',
  ],
  constraints: [
    'Each input has exactly one valid answer.',
    'The same element may not be used twice.',
  ],
  examples: [
    {
      input: 'numbers = [2, 7, 11, 15], target = 9',
      output: '[0, 1]',
      explanation: 'numbers[0] + numbers[1] = 2 + 7 = 9.',
    },
  ],
  languages: [
    {
      id: 'csharp',
      name: 'C#',
      editorLanguage: 'csharp',
      canExecute: true,
      starterFiles: [
        {
          path: 'Solution.cs',
          content:
            'namespace Challenge;\n\npublic static class Solution\n{\n    // TODO\n    throw new NotImplementedException();\n}\n',
        },
      ],
    },
    {
      id: 'python',
      name: 'Python',
      editorLanguage: 'python',
      canExecute: true,
      starterFiles: [
        {
          path: 'solution.py',
          content:
            'def two_sum(numbers: list[int], target: int) -> list[int]:\n    raise NotImplementedError("Implement two_sum")\n',
        },
      ],
    },
  ],
  publicTests: [
    { name: 'Example 1', description: 'The first pair adds up to the target.' },
    { name: 'Example 2', description: 'The matching pair is not at the start.' },
  ],
  evaluation: {
    kind: 'AutomatedTests',
    timeLimitSeconds: 5,
    memoryLimitMb: 256,
  },
  examination: {
    variants: [
      { id: 'standard', label: 'Standard examination', timeLimitSeconds: 600 },
      { id: 'extended', label: 'Extended examination', timeLimitSeconds: 900 },
    ],
  },
  statementMarkdown: [
    '# Two Sum',
    '',
    'Given an array of integers `numbers` and an integer `target`, return the indices of the two numbers that add up to `target`.',
    '',
    '## Approach',
    '',
    'Use a hash map to remember values you have already seen.',
  ].join('\n'),
  hintsMarkdown: '# Hints\n\n1. Try the single-element case.\n2. Store values as you scan.',
}
