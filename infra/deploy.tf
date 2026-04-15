terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = "sa-east-1" 
}

variable "app_domain" {
  type    = string
  default = "sasc26.windisch.com.br"
}

# 1. Security Group - Sem os pontos e vírgulas problemáticos
resource "aws_security_group" "nuclear_sg" {
  name        = "sasc-nuclear-sg"
  description = "Allow SSH, HTTP and HTTPS"

  ingress {
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# 2. O Servidor (Ubuntu 24.04 em sa-east-1)
resource "aws_instance" "app_server" {
  ami           = "ami-01f4c1893b7b17a94" 
  instance_type = "t3.micro"
  vpc_security_group_ids = [aws_security_group.nuclear_sg.id]
  
  tags = {
    Name = "SASC-Nuclear-Node"
  }
}

# 3. IP Estático
resource "aws_eip" "app_ip" {
  instance = aws_instance.app_server.id
  domain   = "vpc"
}

# 4. Identidade do SES
resource "aws_ses_domain_identity" "sasc_domain" {
  domain = var.app_domain
}

# 5. DKIM (Para não cair no SPAM)
resource "aws_ses_domain_dkim" "sasc_dkim" {
  domain = aws_ses_domain_identity.sasc_domain.domain
}

# OUTPUTS: Copie isso para o seu painel de DNS
output "public_ip" {
  value = aws_eip.app_ip.public_ip
}

output "ses_txt_token" {
  value = aws_ses_domain_identity.sasc_domain.verification_token
}

output "dkim_tokens" {
  value = aws_ses_domain_dkim.sasc_dkim.dkim_tokens
}