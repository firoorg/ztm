pipeline {
    agent {
        docker { image 'zcoinofficial/ztm-builder:latest' }
    }

    stages {
        stage('Setup') {
            steps {
                sh 'git clean -d -f -f -q -x'
                sh 'git submodule init'
                sh 'git submodule update --recursive'
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
