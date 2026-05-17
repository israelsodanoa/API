'use strict';

const defaultTopkValues = [1, 5, 10, 20, 30, 50];
const topkValues = parseTopkValues(process.env.TOPK_VALUES);

function parseTopkValues(raw) {
  if (!raw || typeof raw !== 'string') {
    return defaultTopkValues;
  }

  const parsed = raw
    .split(',')
    .map((x) => Number.parseInt(x.trim(), 10))
    .filter((x) => Number.isInteger(x) && x > 0);

  return parsed.length > 0 ? parsed : defaultTopkValues;
}

function pickTopK(context, events, done) {
  const idx = Math.floor(Math.random() * topkValues.length);
  context.vars.topk = topkValues[idx];
  done();
}

function validateTopKResponse(requestParams, response, context, events, done) {
  if (response.statusCode !== 200) {
    return done(new Error(`Expected status 200, got ${response.statusCode}`));
  }

  let body;
  try {
    body = typeof response.body === 'string' ? JSON.parse(response.body) : response.body;
  } catch (err) {
    return done(new Error(`Response is not valid JSON: ${err.message}`));
  }

  if (!Array.isArray(body)) {
    return done(new Error('Response body should be a JSON array of stories'));
  }

  const expectedTopK = Number.parseInt(context.vars.topk || '30', 10);

  if (body.length > expectedTopK) {
    return done(new Error(`Response returned ${body.length} items, expected at most ${expectedTopK}`));
  }

  for (let i = 0; i < body.length; i++) {
    const item = body[i];

    if (typeof item !== 'object' || item === null) {
      return done(new Error(`Item at index ${i} is not an object`));
    }

    if (typeof item.id !== 'number' || typeof item.score !== 'number' || typeof item.title !== 'string') {
      return done(new Error(`Item at index ${i} is missing required fields (id, score, title)`));
    }

    if (i > 0 && body[i - 1].score < item.score) {
      return done(new Error('Response is not sorted by score in descending order'));
    }
  }

  context.vars.storyCount = body.length;
  done();
}

function recordStoryCount(requestParams, response, context, events, done) {
  const count = Number.isInteger(context.vars.storyCount) ? context.vars.storyCount : 0;
  events.emit('histogram', 'stories.returned_per_request', count);
  events.emit('counter', 'stories.items_returned_total', count);
  done();
}

module.exports = {
  pickTopK,
  validateTopKResponse,
  recordStoryCount
};
