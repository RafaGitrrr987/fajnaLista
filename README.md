# Szablon z form window

## Opis
Krótki opis co robi aplikacja.

## Konfiguracja
- .NET 4.8
- Menadzer konfiguracji x64


## Odwołania
### Referencje z katalogu C:\AAARafal\GstarCAD\grxsdk
- GcCoreMgd (właściwości->kopia lokalna:False)
- GcDbMg ((właściwości->kopia lokalna:False)
- GcMgd ((właściwości->kopia lokalna:False)
- GrcCAD.interop
- system.drawing
- system.windows.forms (z odwołania -> Framework)
## Jak uruchomić
1. 
2. 
3. 

## git
- 
git status                         # co zmienione?
git add .                          # dodaj wszystkie zmiany
git commit -m "Twoj opis"          # zapisz commit
git push                           # wyślij na GitHub

git log --oneline --graph --decorate
# lub bardzo krótko:
git log --oneline

git restore .                      # wywal zmiany z katalogu roboczego (UNSTAGED)
git reset                          # zdejmij ze stage (STAGED -> UNSTAGED)

# podgląd historii, znajdź <HASH>
git log --oneline --graph --decorate

# bezpiecznie: nowy commit z treścią ze starego commita
git restore --source <HASH> --worktree --staged :/
git commit -m "Restore project to <HASH>"
git push

# twardo: cofnięcie gałęzi (wymaga force przy pushu)
git branch backup-before-reset-$(Get-Date -Format yyyyMMdd-HHmm)

git push --force-with-lease

git status 
git log --oneline
git add .                          
<<<<<<< HEAD
git commit -m "porzadki git clean -fd"          
git push

git reset --hard <HASH>
git status
Krok 2: Usuń wszystkie nieśledzone pliki
git clean -fd
=======
git commit -m "utworzony form1.columns"          
git push
>>>>>>> 4fc9ed1d2c1f8fcb12af9dcb823bd5c8691cf1ae
