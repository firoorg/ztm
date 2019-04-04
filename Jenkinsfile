pipeline {
    agent {
        docker { image 'zcoinofficial/ztm-builder:latest' }
    }

    stages {
        stage('Setup') {
            steps {
                sh 'git clean -d -f -f -q -x'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build src/Ztm.sln'
            }
        }

        stage('Test') {
            steps {
                sh 'for p in src/*.Tests; do dotnet test $p; done;'
            }
        }
    }
}
