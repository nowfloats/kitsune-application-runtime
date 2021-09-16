follow below build steps:

dotnet restore
dotnet tool install -g Amazon.Lambda.Tools --framework netcoreapp3.1
dotnet lambda package --configuration Release --framework netcoreapp3.1 --output-package bin/Release/netcoreapp3.1/publish-dev-kitsune-identifier.zip


follow below publish steps:
//to publish on serverless we need to install serverless and serverless-offline

npm install -g serverless
serverless
serverless --version
npm install serverless-offline --save-dev


//after installing serverless and serverless-offline we can publish it
//check correct profile is set to publish on aws (key and secret) (C:\Users\YOUR_USERNAME\.aws)
// this will use serverless.yml file for configurations

sls deploy 