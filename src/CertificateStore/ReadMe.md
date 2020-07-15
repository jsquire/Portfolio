# Certificate Store

### Summary

This project is intended to serve as a repository for the metadata about certificates used in various applications and services, as well as the tools to help secure and manage them.  The goal of this project is to offer a centralized reference for certificates using standardized information, and a consistent approach to managing them.

**_Important:_** Do not store sensitive certificates directly in this repository.  Certificates other than those intended for local developer use and/or general access should be kept in Azure KeyVault so that they can be properly secured.  To prevent accidentally adding a certificate to the repository, common extensions for certificates containing a private key exist in the ignore file; for the certificate to be added, you'll need to use the _force_ flag.

### Structure

* **root**  
  _The root contains the overall repository configuration files and general structure._
  
* **tools**  
  _The container for various tools to aid in the management of certificates, organized by the area to which they're applicable._

* **certificates**  
  _The container for the certificate metadata, organized in sub-folders by project/product that they are associated with and, often, the purpose of the certificate._
  
