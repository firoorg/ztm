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
                script {
                    def projects = findFiles(glob: 'src/*.Tests')
                    for (int i = 0; i < projects.size(); i++) {
                        sh "dotnet test ${projects[i]}"
                    }
                }
            }
        }
    }
}
