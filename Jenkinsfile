node {
    withEnv(["PUBLISH=${env.WORKSPACE}/build"]) {
        def commit = null
        def base = null

        stage('Setup') {
            // prepare workspace
            checkout scm
            sh 'git clean -d -f -f -q -x'

            // get commits hash
            commit = sh(
                script: 'git rev-parse HEAD',
                returnStdout: true
            ).trim()

            base = sh(
                script: "git rev-list --parents -n 1 ${commit}",
                returnStdout: true
            ).trim().split('\\s+')[2]
        }

        docker.image('zcoinofficial/ztm-builder:latest').inside {
            // build and run unit tests
            stage('Build') {
                sh 'dotnet build src/Ztm.sln'
            }

            stage('Unit Test') {
                sh 'for p in src/*.Tests; do dotnet test $p; done;'
            }

            stage('Publish') {
                sh "dotnet publish -o \"${env.PUBLISH}\" -r linux-musl-x64 -c Release src/Ztm.WebApi"
                sh "dotnet ef migrations script -o \"${env.PUBLISH}/Ztm.Data.Entity.Postgres.sql\" -i -p src/Ztm.Data.Entity.Postgres"
            }
        }

        stage('E2E Test') {
            // spawn container to run ztm first so the external services can join it network
            def ztm = docker.image('alpine:latest').run()

            try {
                // spwan external services
                withEnv(["ZTM_DOCKER_NETWORK=container:${ztm.id}"]) {
                    // we don't want to use the supplied version to prevent malicious input
                    sh "git checkout ${base} docker-compose.yml"
                    sh 'docker-compose up -d'

                    try {
                    } finally {
                        sh 'docker-compose down'
                    }
                }
            } finally {
                ztm.stop()
            }
        }
    }
}
