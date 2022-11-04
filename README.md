# .NET Photo Gallery Web Application Sample with Azure Blob Storage

This sample application creates a web photo gallery that allows you to host and view images through a .NET web frontend. The code sample also includes functionality for deleting images. At the end, you have the option of deploying the infraestrure/application to Azure using Bicep with Github Actions.

![Azure Blob Storage Photo Gallery Web Application Sample .NET](https://github.com/Azure-Samples/storage-blobs-dotnet-webapp/raw/master/images/photo-gallery.png)

## Technologies used
- NET Core 6.0
- Azure Storage emulator
- Azure Storage
- Azure Container Instances
- Azure Bicep
- Github Actions

Azure Blob Storage Photo Gallery Web Application using ASP.NET MVC The sample uses the asynchronous programming model to demonstrate how to call the Storage Service using the Storage .NET client library's asynchronous APIs.

## Running this sample
1. Before you can run this sample, you must have the following prerequisites:
	- The Azure Storage Emulator, which you can download [here](https://go.microsoft.com/fwlink/?linkid=717179&clcid=0x409). You can also read more about [Using the Azure Storage emulator for development](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator).
	- Visual Studio 2022.

2. Open the Azure Storage emulator. Once the emulator is running it will be able to process the images from the application.

3. Clone this repository using Git for Windows (http://www.git-scm.com/), or download the zip file.

4. From Visual Studio, open the **net-photo-gallery.sln** file from the root directory.

5. In Visual Studio Build menu, select **Build Solution** (or Press F6).

6. You can now run and debug the application locally by pressing **F5** in Visual Studio.

## Deploy this sample to Azure

Create a resource group. Later in this quickstart, you'll deploy your Bicep file to this resource group.

    az group create --name myResourceGroup --location "eastus"

Deploy manually the template.

    az deployment group create --resource-group myResourceGroup --template-file <path-to-template>


Deploy Bicep files by using GitHub Actions

Generate deployment credentials

    az ad sp create-for-rbac --name myApp --role contributor --scopes /subscriptions/{subscription-id}/resourceGroups/myResourceGroup --sdk-auth

The output is a JSON object with the role assignment credentials that provide access to your App Service app similar to below. Copy this JSON object for later. You'll only need the sections with the clientId, clientSecret, subscriptionId, and tenantId values.
```json
    {
      "clientId": "<GUID>",
      "clientSecret": "<GUID>",
      "subscriptionId": "<GUID>",
      "tenantId": "<GUID>",
      (...)
    }
```
### Configure the GitHub secrets

Create secrets for your Azure credentials, resource group, and subscriptions.

In GitHub, navigate to your repository.

* Select Security > Secrets and variables > Actions > New repository secret.

* Paste the entire JSON output from the Azure CLI command into the secret's value field. Name the secret `AZURE_CREDENTIALS`.

* Create another secret named `AZURE_RG`. Add the name of your resource group to the secret's value field (exampleRG).

* Create another secret named `AZURE_SUBSCRIPTION`. Add your subscription ID to the secret's value field (example: 90fd3f9d-4c61-432d-99ba-1273f236afa2).


## About the code
The code included in this sample is meant to be a quick start sample for learning about Azure Web Apps and Azure Storage. It is not intended to be a set of best practices on how to build scalable enterprise grade web applications.

## More information
- [What is a Storage Account](http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/)
- [Getting Started with Blobs](http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/)
- [Blob Service Concepts](http://msdn.microsoft.com/en-us/library/dd179376.aspx)
- [Blob Service REST API](http://msdn.microsoft.com/en-us/library/dd135733.aspx)
- [Blob Service C# API](http://go.microsoft.com/fwlink/?LinkID=398944)
