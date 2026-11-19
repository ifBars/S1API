const fs = require('node:fs');
const path = require('node:path');

const rules = {
  docs: ['Review in context', 'Keep one authoritative explanation. Document prerequisites, units and lifecycle timing where a consumer needs them. Docs-only changes can use documentation checks.'],
  'frame-log': ['Contributor action', 'A console call runs on every frame callback. Gate diagnostic output or report an ongoing failure once.'],
  'startup-budget': ['Contributor action', 'This startup path contains more than three straight-line informational calls. Keep one startup message where possible.'],
  'console-info': ['Maintainer cleanup', 'Check who sees this message with verbose logging off. Move routine success and registration details to Log.Debug. Count repeats across items, NPCs and reloads.'],
  diagnostic: ['Review in context', 'Check the diagnostic names the affected operation and remedy. A warning per save load is fine. Suppress repeated failures during short retries while preserving independent failures. Unity Player.log is a separate destination.'],
  'public-api': ['Review in context', 'Check existing mods retain managed wrapper identity, native defaults and null results. Inspect nested types for native leaks. Preserve durable IDs and shipped signatures. Prefer a compatible obsolete shim for replacements. Document units, ownership and defaults. Identify the invariant before restricting a supported use.'],
  lookup: ['Review in context', 'Verify the native lookup on each runtime. Prefer the existing wrapper or helper. Check the need for reflection or reconstructed fallbacks and measure first-use and repeated costs separately for performance claims.'],
  lifecycle: ['Review in context', 'Reproduce through the supported mod lifecycle. Check ownership, cleanup, save reload and host/client behavior where relevant. Record live Mono and IL2CPP evidence separately.'],
  'test-signal': ['Review in context', 'Check what defect this test detects. Existing compile gates can cover a trivial forwarder. A reflection test cannot establish scene, rendering or multiplayer behavior.'],
  scope: ['Maintainer cleanup', 'Keep explanations of interop and lifecycle ordering. Check whether a new wrapper carries behavior or protects an invariant. Trim narration or duplicate instructions if present and keep unrelated edits out of the diff.'],
  persistence: ['Review in context', 'Check existing saves and peers retain IDs, defaults and wire behavior. Exercise the old data or mixed lifecycle path the change affects.'],
  performance: ['Review in context', 'For a performance claim, compare cold and repeated calls and check ownership, invalidation, and behavior after reload.'],
};

module.exports = async ({github, context}) => {
  const {owner, repo} = context.repo;
  const run = context.payload.workflow_run;
  const number = Number(process.env.PR_NUMBER);
  const report = path.join(process.env.RUNNER_TEMP, 'review.json');
  if (!fs.existsSync(report)) return;
  const {data: pr} = await github.rest.pulls.get({owner, repo, pull_number: number});
  if (pr.state !== 'open' || pr.head.sha !== run.head_sha) return;
  const findings = JSON.parse(fs.readFileSync(report, 'utf8'));
  const jobs = await github.paginate(github.rest.actions.listJobsForWorkflowRun, {owner, repo, run_id: run.id, filter: 'latest'});
  const steps = jobs.find(job => job.name === 'documentation')?.steps || [];
  const marker = '<!-- s1api-maintainer-review -->';
  const lines = [marker, '### S1API maintainer review', '',
    'Reviewed ' + pr.head.sha.slice(0, 8) + '. [CI results and approval instructions](' + run.html_url + ')'];
  for (const name of ['Check public API compatibility', 'Compare existing consumer source contracts', 'Check maintainer review policy']) {
    const step = steps.find(s => s.name === name);
    lines.push('- ' + name + ' — ' + (step?.conclusion || 'unavailable'));
  }
  for (const group of ['Contributor action', 'Maintainer cleanup', 'Review in context']) {
    const relevant = Object.entries(rules).filter(([key, [section]]) => section === group && findings.some(f => f.rule === key));
    if (!relevant.length) continue;
    lines.push('', '**' + group + '**', '');
    for (const [key, [, message]] of relevant) {
      const sites = findings.filter(f => f.rule === key);
      const links = sites.slice(0, 3).map(f => {
        const urlPath = f.path.split('/').map(s => encodeURIComponent(s).replace(/[()]/g, c => '%' + c.charCodeAt(0).toString(16))).join('/');
        return '[L' + f.line + '](https://github.com/' + owner + '/' + repo + '/blob/' + pr.head.sha + '/' + urlPath + '#L' + f.line + ')';
      });
      lines.push('- ' + message + ' ' + links.join(', ') + (sites.length > 3 ? ' (+' + (sites.length - 3) + ' more)' : ''));
    }
  }
  const body = lines.join('\n');
  const comments = await github.paginate(github.rest.issues.listComments, {owner, repo, issue_number: number, per_page: 100});
  const previous = comments.find(c => c.user?.login === 'github-actions[bot]' && c.body?.startsWith(marker));
  if (!previous) await github.rest.issues.createComment({owner, repo, issue_number: number, body});
  else if (previous.body !== body) await github.rest.issues.updateComment({owner, repo, comment_id: previous.id, body});
};
