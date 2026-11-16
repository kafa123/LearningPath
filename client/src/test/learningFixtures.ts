import type {
  LearningPathSummary,
  MaterialDetail,
  ModuleDetail,
} from '@/shared/api/learning'

export const learningPathFixture: LearningPathSummary = {
  slug: 'programming-fundamentals',
  name: 'Programming Fundamentals',
  description: 'Algorithms, data structures, and the core techniques behind reliable programs.',
  modules: [
    {
      slug: 'algorithm-foundations',
      title: 'Algorithm Foundations',
      summary: 'Turn a problem statement into a correct, readable sequence of steps.',
      level: 'Beginner',
      estimatedMinutes: 45,
      prerequisiteSlugs: [],
      competencySlugs: ['algorithm-decomposition', 'correctness-reasoning'],
    },
    {
      slug: 'complexity-analysis',
      title: 'Complexity Analysis',
      summary: 'Compare algorithms by how their work grows as the input grows.',
      level: 'Intermediate',
      estimatedMinutes: 50,
      prerequisiteSlugs: ['algorithm-foundations'],
      competencySlugs: ['asymptotic-analysis', 'performance-tradeoffs'],
    },
  ],
}

export const moduleFixture: ModuleDetail = {
  slug: 'complexity-analysis',
  title: 'Complexity Analysis',
  summary: 'Compare algorithms by how their work grows as the input grows.',
  level: 'Intermediate',
  estimatedMinutes: 50,
  domain: { slug: 'programming-fundamentals', name: 'Programming Fundamentals' },
  objectives: [
    'Count the dominant operation in a loop or recursive call.',
    'Compare algorithms by asymptotic growth instead of raw timing.',
  ],
  materials: [
    {
      slug: 'complexity-analysis-lesson',
      title: 'Measuring growth with Big-O',
      kind: 'Lesson',
      estimatedMinutes: 20,
    },
  ],
  competencies: [
    {
      slug: 'asymptotic-analysis',
      name: 'Asymptotic analysis',
      description: 'Estimate time and space growth using Big-O notation.',
    },
  ],
  prerequisites: [
    {
      slug: 'algorithm-foundations',
      title: 'Algorithm Foundations',
      domainSlug: 'programming-fundamentals',
    },
  ],
}

export const materialFixture: MaterialDetail = {
  slug: 'complexity-analysis-lesson',
  title: 'Measuring growth with Big-O',
  kind: 'Lesson',
  estimatedMinutes: 20,
  module: {
    slug: 'complexity-analysis',
    title: 'Complexity Analysis',
    domainSlug: 'programming-fundamentals',
  },
  bodyMarkdown: [
    '## Measuring growth, not seconds',
    '',
    'Big-O describes how the work grows as the input grows.',
    '',
    '| Class | Example |',
    '| --- | --- |',
    '| O(n) | one pass over a collection |',
    '',
    '```text',
    'for each item in collection',
    '```',
  ].join('\n'),
}

export function jsonResponse(body: unknown, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as Response
}
