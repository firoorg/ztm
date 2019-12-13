const core = require('@actions/core');
const exec = require('@actions/exec');
const fs = require('fs');
const path = require('path');

async function runTest(project, options = {}) {
  // run tests
  let args = ['test'];

  if (!options.build) {
    args.push('--no-build');
  }

  if (options.verbose) {
    args.push('-v');
    args.push(options.verbose);
  }

  if (options.coverage && options.coverage.result) {
    args.push('--collect');
    args.push('Code Coverage');
    args.push('-r');
    args.push(options.coverage.result);
  }

  args.push(project);

  await exec.exec('dotnet', args);

  // process coverage report
  if (options.coverage && options.coverage.result) {
    let container = fs.readdirSync(options.coverage.result)[0];
    let coverage = fs.readdirSync(container)[0];

    await exec.exec(options.coverage.tool, [
      'analyze',
      `/output:${options.coverage.report}`,
      path.join(options.coverage.result, container, coverage)
    ]);

    fs.rmdirSync(path.join(options.coverage.result, container), { recursive: true });
  }
}

async function run() {
  try {
    let projects = core.getInput('projects', { required: true });
    let build = core.getInput('build', { required: true }) === 'true';
    let verbose = core.getInput('verbose', { required: true });
    let result = core.getInput('result', { required: false });
    let coverage = core.getInput('coverage-tool', { required: false });
    let report = core.getInput('report', { required: false });

    for (let file of fs.readdirSync(projects, { withFileTypes: true })) {
      if (!file.isDirectory() || !/.+\.Tests$/.test(file.name)) {
        continue;
      }

      await runTest(path.join(projects, file.name), {
        build: build,
        verbose: verbose,
        coverage: {
          result: result,
          tool: coverage,
          report: path.join(report, `${file.name}.coveragexml`)
        }
      });
    }
  } catch (error) {
    core.setFailed(error.message);
  }
}

run();
