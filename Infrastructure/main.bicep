// az deployment sub create --location eastus --name order-proc --template-file .\main.bicep --parameters unique=waymack
@description('A lowercase letter and number combination that is used to make resource names unique.')
@maxLength(13)
param unique string

@description('Region for deployment. Defaults to deployment location.')
param location string = deployment().location

var prefix = 'order-proc-${unique}'

targetScope = 'subscription'

resource rg 'Microsoft.Resources/resourceGroups@2021-01-01' = {
  name: '${prefix}-rg'
  location: location
}

module environment './environment.bicep' = {
  name: prefix
  scope: rg
  params: {
    prefix: prefix
    location: location
  }
}

output resourceGroup string = rg.name
output functionApp string = environment.outputs.functionAppUrl
