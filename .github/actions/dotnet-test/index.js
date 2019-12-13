const core = require('@actions/core');
const exec = require('@actions/exec');
const fs = require('fs');
const path = require('path');

async function runTest(project, options = {}) {
  // run tests
  let result = options.coverage ? options.coverage.result : undefined;
  let args = ['test'];

  if (!options.build) {
    args.push('--no-build');
  }

  if (options.verbose) {
    args.push('-v');
    args.push(options.verbose);
  }

  if (result) {
    args.push('--collect');
    args.push('Code Coverage');
    args.push('-r');
    args.push(result);
  }

  args.push(project);

  await exec.exec('dotnet', args);

  // process coverage report
  if (result) {
    let container = fs.readdirSync(result)[0];
    let coverage = fs.readdirSync(path.join(result, container))[0];

    await exec.exec(options.coverage.tool, [
      'analyze',
      `/output:${options.coverage.report}`,
      path.join(result, container, coverage)
    ]);

    fs.rmdirSync(path.join(result, container), { recursive: true });
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
