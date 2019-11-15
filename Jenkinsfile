node {
    def workspace = env.WORKSPACE
    def buildTag = env.BUILD_TAG
    def publish = "${workspace}/build"
    def compose = null

    stage('Setup') {
        // checkout source
        checkout scm

        // revert untrusted files to the base version and backup it before we execute any untrusted code so the attacker
        // don't have a chance to put a malicious content
        def latest = sh(
            script: 'git rev-parse HEAD',
            returnStdout: true
        ).trim()

        sh "git checkout origin/${env.CHANGE_TARGET}"

        compose = readFile('docker-compose.yml')

        // switch back to latest commit
        sh "git checkout ${latest}"
        sh 'git clean -d -f -f -q -x'
    }

    // build, run unit tests and publish application
    docker.image('zcoinofficial/ztm-builder:latest').inside {
        stage('Build') {
            sh 'dotnet build src/Ztm.sln'
        }

        stage('Unit Test') {
            sh 'for p in src/*.Tests; do dotnet test $p; done;'
        }

        stage('Publish') {
            sh "dotnet publish -o \"${publish}\" -r linux-musl-x64 -c Release src/Ztm.WebApi"

            // we need a dummy value for ZTM_MAIN_DATABASE just to let the code pass, it does not use when we
            // generate script
            withEnv(['ZTM_MAIN_DATABASE=Host=127.0.0.1;Database=postgres;Username=postgres']) {
                sh "dotnet ef migrations script -o \"${publish}/Ztm.Data.Entity.Postgres.sql\" -i -p src/Ztm.Data.Entity.Postgres"
            }
        }
    }

    stage('E2E Test') {
        // create an isolated network for e2e tests
        def net = sh(
            script: "docker network create ${buildTag}",
            returnStdout: true
        ).trim()

        try {
            // spawn external services
            def mainDb = "${buildTag}-db-main"
            def zcoind = "${buildTag}-zcoind"

            writeFile(file: 'docker-compose.yml', text: compose)

            withEnv(["ZTM_MAIN_DATABASE_CONTAINER=${mainDb}", "ZTM_ZCOIND_CONTAINER=${zcoind}", "ZTM_DOCKER_NETWORK=${net}"]) {
                sh 'docker-compose up -d'
            }

            // modify ztm's configurations
            def conf = readJSON(file: "${publish}/appsettings.json")

            conf.Logging.LogLevel.Default = 'Information'
            conf.Database.Main.ConnectionString = "Host=${mainDb};Database=postgres;Username=postgres"
            conf.Zcoin.Network.Type = 'Regtest'
            conf.Zcoin.Rpc.Address = "http://${zcoind}:28888"
            conf.Zcoin.ZeroMq.Address = "tcp://${zcoind}:28332"

            writeJSON(
                json: conf,
                file: "${publish}/appsettings.json",
                pretty: 2
            )

            try {
                // start ztm
                def uid = sh(script: 'id -u', returnStdout: true).trim()
                def gid = sh(script: 'id -g', returnStdout: true).trim()
                def ztm = docker.image('alpine:latest').run("--user=${uid}:${gid} --network=${net} -v ${publish}:/opt/ztm:ro -w /opt/ztm", "/opt/ztm/Ztm.WebApi --urls=http://*:5000")

                try {
                    // environment is ready, start e2e tests
                    docker.image('zcoinofficial/ztm-builder:latest').inside("--network=${net} -e ZTM_HOST=${ztm.id} -e ZTM_PORT=5000") {
                        sh 'dotnet test src/Ztm.EndToEndTests'
                    }
                } finally {
                    ztm.stop()
                }
            } finally {
                sh 'docker-compose down'
            }
        } finally {
            sh "docker network rm ${net}"
        }
    }
}
