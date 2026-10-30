# Plays the Holo-Duel's rules (Assets/Scripts/HoloDuel.cs) many times: three kinds of player against the rival's
# easy AI, to set how often the rival slips and how long a game runs. Usage: python Tools/holo_sim.py
# An attack: both roll a die, the kind that beats the other adds Bonus; the higher hurts the other one life, a tie
# hurts no one. A piece with no lives left leaves the board, and an attacker that beat it takes its cell.
import random

BONUS, LIVES, BOLD = 2, 2, 6

def nxt(c):
    if c == 0: return [1, 2, 3, 4, 5, 6]
    k6 = (c - 1) % 6; ring = 1 if c <= 6 else 7
    out = [ring + (k6 + 1) % 6, ring + (k6 + 5) % 6, c + 6 if ring == 1 else c - 6]
    if ring == 1: out.append(0)
    return out

def edge(a, d): return BONUS if (a + 1) % 3 == d else -BONUS if (d + 1) % 3 == a else 0

def odds(a, d):  # (win, tie, lose) for the attacker
    w = t = 0
    for x in range(1, 7):
        for y in range(1, 7):
            s = x + edge(a, d) - y
            w += s > 0; t += s == 0
    return w / 36, t / 36, (36 - w - t) / 36

def moves(board, mine):
    return [(c, to) for c, p in board.items() if p[1] == mine for to in nxt(c) if board.get(to) is None or board[to][1] != mine]

def score(board, c, to, mine, noise):
    kind = board[c][0]; there = board.get(to)
    s = random.uniform(0, noise)
    if there:
        w, t, l = odds(kind, there[0])
        s += 10 * w - 10 * l + (BOLD if not mine else 0)  # the rival's boldness (HoloDuel.boldness)
    for n in nxt(to):
        near = board.get(n)
        if not near or near[1] == mine or n == to: continue
        w, t, l = odds(near[0], kind)
        s += -6 * w + 3 * l
    return s

def pick(board, mine, style, mistakes):
    opts = moves(board, mine)
    if not opts: return None
    if style == 'random' or (style == 'rival' and random.random() < mistakes): return random.choice(opts)
    if style == 'hints':  # attacks what the board shows green (better than even), keeps off red, else wanders
        green = [o for o in opts if o[1] in board and odds(board[o[0]][0], board[o[1]][0])[0] > 0.5]
        if green: return random.choice(green)
        quiet = [o for o in opts if o[1] not in board]
        return random.choice(quiet or opts)
    return max(opts, key=lambda o: score(board, *o, mine, 2 if style == 'rival' else 0.5))

def game(style, mistakes=0.5, limit=60):
    board = {}
    for kind, (a, b) in enumerate(zip(random.sample([10, 9, 11, 4], 3), random.sample([7, 8, 12, 1], 3))):
        board[a] = [kind, True, LIVES]; board[b] = [kind, False, LIVES]
    mine_turn, n, rolls = True, 0, 0
    while True:
        m = pick(board, mine_turn, style if mine_turn else 'rival', mistakes)
        if m:
            c, to = m; p = board[c]; there = board.get(to)
            if there is None:
                board[to] = board.pop(c)
            else:
                rolls += 1
                s = random.randint(1, 6) + edge(p[0], there[0]) - random.randint(1, 6)
                if s > 0:
                    there[2] -= 1
                    if there[2] <= 0: del board[to]; board[to] = board.pop(c)
                elif s < 0:
                    p[2] -= 1
                    if p[2] <= 0: del board[c]
        n += 1
        mine = sum(1 for v in board.values() if v[1]); theirs = len(board) - mine
        if mine == 0 or theirs == 0 or n >= limit:
            ml = sum(v[2] for v in board.values() if v[1]); tl = sum(v[2] for v in board.values() if not v[1])
            return mine > 0 and (theirs == 0 or ml > tl), n, rolls  # a tie at the limit is a loss: play it again
        mine_turn = not mine_turn

if __name__ == '__main__':
    random.seed(1)
    print('odds with the edge:', odds(0, 1), ' even:', odds(0, 0), ' against:', odds(1, 0))
    for style in ['random', 'hints', 'smart']:
        res = [game(style) for _ in range(4000)]
        print(f"{style:7s} wins {sum(r[0] for r in res)/len(res):5.1%}  moves {sum(r[1] for r in res)/len(res):4.1f}  dice rolls {sum(r[2] for r in res)/len(res):4.1f}  hit limit {sum(r[1]>=60 for r in res)/len(res):.1%}")
