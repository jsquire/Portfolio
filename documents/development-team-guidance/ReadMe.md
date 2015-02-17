# Development Team Guidance #

### Overview ###

Included in this section are documents intended to serve as resources for a development team.

### Items ###

* **developer-introduction.docx**
  <br />_This document is intended to serve as an on-boarding resource for new developers hired into a .NET-centric development group that operates as a shared services organization within an enterprise.  Team norms, development methodologies, technical environment details, and development tool sets are discussed at a high level to help familiarize new hires with the team and its role in the organization._
  
* **coding-conventions.docx**
  <br />_This document details a set of coding conventions for C# development.  First written in late 2005, it is intended to provide a very lightweight and high-level set of common conventions to promote code consistency across a team.  As coding conventions are often a source of friction for a team, every attempt was made to frame these conventions as suggestion and empower team members to concentrate on code and not draconian standards.  To conform to common industry idioms, this document was heavily adapted from published coding practices at Microsoft and attempts to stay within expected norms._
  
* **subversion-source-control-practices.docx**
  <br />_This document outlines a set of practices for development using the Subversion source control system.  While a slight bias towards .NET development can be seen, the underlying guidance is relatively applicable across development platforms.  This document was written as an aid for a relatively inexperienced development team moving and an existing codebase from Visual Source Safe to Subversion._<br />
  <br />_To that point, the team had been working in an exclusive checkout model in which the common practice was to work on a particular feature until complete and committing it to source control as a single monolithic unit.  Contention for shared files was high, and developers were often blocked by their peers until a feature was complete.  The practices detailed in this document aimed to help the team learn to adapt to an edit-merge-commit work flow while working in more granular units with more frequent commits - a practice which many were uneasy and concerned with coming from VSS._<br />
  <br />_This set of guidance was the result of an evaluation and review of source code control canddiates, which can be found in the result of this evaluation was the adoption of Subversion.  The team guidance for the standards and process around it can be found in the [Source Code Control Evaluation](../project-related/source-control-candidate-evaluation.docx "Source Code Control Candidate Evaluation") document._
  
* **estimate-sheet-template.mpp**
  <br />_This document defines a template for a formal and granular estimate which could be used at the basis for a formal project plan.  Created in collaboration effort with other architects and project managers, it was intended to provide a uniform and well-understood set of tasks to help estimation be more consistent across development groups and more familiar to account managers presenting it to clients._<br />
  <br />_Examples using this template are provided with the sample [estimates](../estimates "estimates")._