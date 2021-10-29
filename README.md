# 3301Analysis

A collection of programs for running automatic decryption attempts against the unsolved Liber Primus cryptographic puzzle of Cicada 3301.

## Usage

Clone the repository.

`git clone https://github.com/Rusty3141/3301Analysis.git`

### AutoDecrypt

`cd AutoDecrypt/`

#### Run an example decryption on the solved pages

`dotnet run`

#### Create custom jobs

`dotnet run -m`

Follow the interactive prompts to generate a custom JobSettings JSON file. All decryption attempts are interpreted using a custom postfix grammar. Grammar tokens can be viewed and changed by extending the class `Grammar` in `AutoDecrypt/modules/language/Grammar.cs`.

For example, the postfix expression `p i prime 1 - -` (=`p i prime φ -`) decrypts 56.jpg/section 14 and `p` decrypts 57.jpg/section 15.

#### Run custom jobs

`dotnet run -d <path to JSON file relative to "_data/jobs/">`

##### Example: `dotnet run -d SolvedPagesDecryptions.json`
