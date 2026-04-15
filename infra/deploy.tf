terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# 1. Configure the AWS Provider (São Paulo region for lowest latency to the campus)
provider "aws" {
  region = "sa-east-1" 
}

# 2. Variables for your specific domain
variable "app_domain" {
  description = "The subdomain for the app and emails"
  type        = string
  default     = "sasc26.windisch.com.br" 
}

# 3. Set up a Security Group to allow web and SSH traffic
resource "aws_security_group" "web_sg" {
  name        = "sasc-checkin-sg"
  description = "Allow HTTP, HTTPS, and SSH traffic"

  ingress {
    description = "SSH from anywhere"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "HTTP from anywhere"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "HTTPS from anywhere"
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

# 4. Find the latest Ubuntu 24.04 LTS AMI (Free Tier Eligible)
data "aws_ami" "ubuntu" {
  most_recent = true
  owners      = ["099720109477"] # Canonical's official AWS account ID

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd-gp3/ubuntu-noble-24.04-amd64-server-*"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# 5. Provision the EC2 Instance (Free Tier)
resource "aws_instance" "app_server" {
  ami           = data.aws_ami.ubuntu.id
  instance_type = "t2.micro" # Free tier eligible
  
  # IMPORTANT: Add your AWS Key Pair name here so you can SSH into the server later!
  # key_name      = "your-aws-key-pair-name" 

  vpc_security_group_ids = [aws_security_group.web_sg.id]

  # User Data script: Automatically runs on first boot to install .NET 8 and Nginx
  user_data = <<-EOF
              #!/bin/bash
              apt-get update -y
              apt-get install -y wget apt-transport-https software-properties-common
              wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
              dpkg -i packages-microsoft-prod.deb
              rm packages-microsoft-prod.deb
              apt-get update -y
              apt-get install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0 nginx
              EOF

  tags = {
    Name = "SASC-CheckIn-Server"
  }
}

# 6. Attach an Elastic IP (Static IP) to your EC2 instance
resource "aws_eip" "app_ip" {
  instance = aws_instance.app_server.id
  domain   = "vpc"

  tags = {
    Name = "SASC-Static-IP"
  }
}

# 7. Configure SES for the entire Subdomain
resource "aws_ses_domain_identity" "sasc_domain" {
  domain = var.app_domain
}

# 8. Generate DKIM tokens for email deliverability (Anti-Spam)
resource "aws_ses_domain_dkim" "sasc_dkim" {
  domain = aws_ses_domain_identity.sasc_domain.domain
}

# 9. Outputs: The exact DNS records you need to copy-paste to your Domain Registrar
output "step_1_a_record" {
  description = "Point your subdomain to this IP address"
  value       = "Type: A | Name: sasc26 | Content: ${aws_eip.app_ip.public_ip}"
}

output "step_2_ses_verification_txt" {
  description = "TXT record to prove you own the domain to AWS"
  value       = "Type: TXT | Name: _amazonses.sasc26 | Content: ${aws_ses_domain_identity.sasc_domain.verification_token}"
}

output "step_3_dkim_cname_records" {
  description = "Create these 3 CNAME records to prevent emails going to spam"
  value = [
    for token in aws_ses_domain_dkim.sasc_dkim.dkim_tokens :
    "Type: CNAME | Name: ${token}._domainkey.sasc26 | Content: ${token}.dkim.amazonses.com"
  ]
}