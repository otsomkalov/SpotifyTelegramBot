terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=3.78.0"
    }
  }
}

provider "azurerm" {
  features {}
}

locals {
  tags = {
    env  = var.env
    name = "spotify-telegram-bot"
  }
}

resource "azurerm_resource_group" "rg-spotify-telegram-bot" {
  name     = "rg-spotify-telegram-bot-${var.env}"
  location = "France Central"

  tags = local.tags
}

resource "azurerm_application_insights" "appi-spotify-telegram-bot" {
  resource_group_name = azurerm_resource_group.rg-spotify-telegram-bot.name
  location            = azurerm_resource_group.rg-spotify-telegram-bot.location

  name             = "appi-spotify-telegram-bot-${var.env}"
  application_type = "web"
}

resource "azurerm_storage_account" "st-spotify-telegram-bot" {
  resource_group_name = azurerm_resource_group.rg-spotify-telegram-bot.name
  location            = azurerm_resource_group.rg-spotify-telegram-bot.location

  name                     = "stspotifytgbot${var.env}"
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "Storage"

  tags = local.tags
}

resource "azurerm_service_plan" "asp-spotify-telegram-bot" {
  resource_group_name = azurerm_resource_group.rg-spotify-telegram-bot.name
  location            = azurerm_resource_group.rg-spotify-telegram-bot.location

  name     = "asp-spotify-telegram-bot-${var.env}"
  os_type  = "Linux"
  sku_name = "Y1"

  tags = local.tags
}

resource "azurerm_linux_function_app" "func-spotify-telegram-bot" {
  resource_group_name = azurerm_resource_group.rg-spotify-telegram-bot.name
  location            = azurerm_resource_group.rg-spotify-telegram-bot.location

  storage_account_name       = azurerm_storage_account.st-spotify-telegram-bot.name
  storage_account_access_key = azurerm_storage_account.st-spotify-telegram-bot.primary_access_key
  service_plan_id            = azurerm_service_plan.asp-spotify-telegram-bot.id

  name = "func-spotify-telegram-bot-${var.env}"

  functions_extension_version = "~4"
  builtin_logging_enabled     = false

  site_config {
    application_insights_key = azurerm_application_insights.appi-spotify-telegram-bot.instrumentation_key
    app_scale_limit          = 10

    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = {
    Telegram__Token            = var.telegram-token
    Telegram__BotUrl           = var.telegram-bot-url
    Spotify__ClientId          = var.spotify-client-id
    Spotify__ClientSecret      = var.spotify-client-secret
    Spotify__CallbackUrl       = var.spotify-callback-url
    Database__ConnectionString = var.database-connection-string
    Database__Name             = var.database-name
  }

  tags = local.tags
}
