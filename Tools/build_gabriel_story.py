import json
from pathlib import Path
rows=[]
def add(k,es,en,pt,ja):rows.append(dict(zip(['key','es','en','pt','ja'],[k,es,en,pt,ja])))
def section(c,side,lines):
 for n,line in enumerate(lines,1):add(f'story.gabriel.{c}.{side}.{n}',*line)
for c,line in enumerate([
 ['Sin alas y sin vacaciones','No wings, no holidays','Sem asas, sem férias','翼も休暇もなし'],
 ['Una derrota por amistad','Losing for a friend','Uma derrota por amizade','友のための敗北'],
 ['Ni un día libre','Not a single day off','Nem um dia de folga','休日は一日もなし'],
 ['Cuando termine mi turno','When my shift ends','Quando meu turno acabar','勤務が終わったら'],
]):add(f'story.gabriel.{c}.title',*line)
add('hero.character.gabriel','Gabriel','Gabriel','Gabriel','ガブリエル')
section(0,'intro',[
 ['Ser un arcángel y nacer sin alas suena a vacaciones de por vida, ¿verdad? Si no puedes salir volando a luchar, alguien tendrá que dejarte descansar.','Being an archangel born without wings sounds like a lifetime holiday, right? If you cannot fly off to fight, surely someone will let you rest.','Ser um arcanjo e nascer sem asas parece férias para sempre, né? Se não pode voar para lutar, alguém deve deixar você descansar.','翼のない大天使として生まれたら、一生休暇だと思うでしょ？飛んで戦えないなら、休ませてもらえるって。'],
 ['Pues me presento: soy Gabriel, la mujer con la peor suerte del universo. Vieron mi caso y pensaron: qué buena oportunidad para encargarle la defensa desde una locomotora.','Allow me to introduce myself: Gabriel, the unluckiest woman in the universe. They saw my situation and thought: perfect, she can defend heaven from a locomotive.','Então me apresento: Gabriel, a mulher mais azarada do universo. Viram meu caso e pensaram: perfeito, ela pode defender o céu numa locomotiva.','自己紹介するね。宇宙で一番ついてない女、ガブリエル。私を見て、皆こう思ったの。「列車で天界を守らせればいいじゃない」。'],
 ['Y todavía dicen: qué trabajo tan fácil tiene Gabriel. ¡Para nada! Todos los días aparece un demonio, un monstruo o algún loco religioso que quiere conquistar el cielo.','And they still say: Gabriel has such an easy job. Not even close! Every day some demon, monster or religious lunatic comes to conquer heaven.','E ainda dizem: que trabalho fácil a Gabriel tem. Nada disso! Todo dia chega um demônio, monstro ou fanático querendo conquistar o céu.','それでも「ガブリエルは楽な仕事でいいね」だって。全然！毎日、悪魔や怪物や変な信者が天界を征服しに来るんだから。'],
 ['No tengo ni un momento para mí. Mi Gora-P y yo llevamos tantas guardias que ya reconozco a los invasores por cómo pisan los rieles.','I never get a moment to myself. Gora-P and I have worked so many shifts I recognise invaders by their footsteps on the rails.','Não tenho um minuto para mim. Eu e Gora-P fizemos tantos plantões que reconheço os invasores pelo jeito de pisar nos trilhos.','自分の時間なんてない。ゴラＰと当直をしすぎて、線路を踏む音で侵入者がわかるくらい。'],
 ['Encima soy la rara del cielo por vestirme como una gal. Perdón por querer verme bien mientras hago el trabajo que nadie más quiere hacer.','And apparently I am the weird one for dressing like a gal. Sorry for wanting to look good while doing the job nobody else wants.','E ainda sou a estranha do céu por me vestir como uma gal. Desculpem querer ficar bonita enquanto faço o trabalho que ninguém quer fazer.','しかもギャルっぽい服だから天界の変わり者扱い。誰もやりたがらない仕事でも、おしゃれくらいしたいんですけど。'],
 ['Como les decía: aquí no hay días libres. La alarma acaba de sonar. Parece que una demonio vino a visitarnos. Vamos, Gora-P... otro turno tranquilo.','Like I said: no days off here. There goes the alarm. A demon woman has come to visit. Come on, Gora-P... another quiet shift.','Como eu disse: aqui não tem folga. Aí está o alarme. Uma demônia veio visitar. Vamos, Gora-P... mais um plantão tranquilo.','ほら、休みなんてないでしょ。もう警報。悪魔の女の子が来たみたい。行こう、ゴラＰ……今日も平和な当直だね。'],
])
section(0,'before',[
 ['Solo vengo por una reliquia. Si te apartas, acabamos antes.','I only came for a relic. Move aside and we finish sooner.','Só vim por uma relíquia. Sai da frente e terminamos logo.','遺物を取りに来ただけ。どけば早く済むわ。'],
 ['Eso me dicen todos. Luego rompen la puerta y a mí me toca rellenar el informe.','Everyone says that. Then they break the gate and I get the paperwork.','Todo mundo diz isso. Depois quebra o portão e sobra o relatório para mim.','皆そう言うの。その後、門を壊して、報告書を書くのは私。'],
 ['Entonces procura escribir que peleé bastante bien.','Then make sure you write that I fought well.','Então escreve que eu lutei muito bem.','なら、私の戦いぶりもちゃんと書いてね。'],
])
section(0,'after',[
 ['¡Una menos! A ver si ahora puedo sentarme cinco minutos.','One down! Maybe now I can sit for five minutes.','Uma a menos! Quem sabe agora posso sentar cinco minutos.','一人片付いた！今度こそ五分くらい座れるかな。'],
 ['Todavía me falta hablar con la guardiana. Esto no termina aquí.','I still need to speak with the guardian. This is not over.','Ainda preciso falar com a guardiã. Isso não acaba aqui.','まだ守護者に用があるの。これで終わりじゃないわ。'],
])
section(0,'outro',[
 ['Claro. Yo hago el trabajo difícil y después la guardiana de muchos ojos se queda con el crédito de la pelea final. Bla, bla, bla... qué gran defensa del cielo.','Of course. I do the hard work, then the many-eyed guardian gets the credit for the final fight. Blah, blah... what a glorious defence of heaven.','Claro. Eu faço o trabalho difícil e a guardiã de muitos olhos leva o crédito da luta final. Blá, blá... que bela defesa do céu.','やっぱりね。苦労するのは私、最後の戦いで手柄を取るのは多眼の守護者。はいはい、天界を守った英雄ですね。'],
 ['¿Qué les dije? Ya viene otra. No apaguen la alarma, que así nos ahorramos volver a encenderla.','What did I tell you? Here comes another. Leave the alarm on; it saves switching it back on.','O que eu disse? Lá vem outra. Deixa o alarme ligado para não ter que ligar de novo.','ほら、言ったとおり。また来た。警報は切らなくていいよ。すぐ鳴らすんだから。'],
])
section(1,'intro',[
 ['Un momento... conozco a esa chica. Es Tzmil. Y ese bulto que trae es mi amigo de la infancia. Más le vale no estar durmiendo otra vez.','Wait... I know her. Tzmil. And that lump she is carrying is my childhood friend. He had better not be sleeping again.','Espera... conheço essa garota. Tzmil. E aquele peso que carrega é meu amigo de infância. Espero que não esteja dormindo de novo.','待って……あの子、ツミルだ。背負ってるの、幼なじみじゃん。また寝てるだけじゃないでしょうね。'],
])
section(1,'before',[
 ['Gabriel, necesito llevar a mi hermano con nuestro padre. Por favor, déjanos pasar.','Gabriel, I need to take my brother to our father. Please let us through.','Gabriel, preciso levar meu irmão ao nosso pai. Deixe a gente passar, por favor.','ガブリエル、兄を父のところへ連れていきたいの。通してくれない？'],
 ['Tzmil... tengo órdenes de detener a cualquiera que cruce. Dame una buena excusa para perder esta pelea.','Tzmil... my orders are to stop anyone crossing. Give me a good excuse to lose this fight.','Tzmil... mandaram deter quem atravessar. Me dê uma boa desculpa para perder esta luta.','ツミル……通る人は全員止めろって命令なの。私が負ける口実、作ってよ。'],
 ['Creo que puedo ayudarte con eso.','I think I can help with that.','Acho que posso ajudar com isso.','それなら、手伝えると思う。'],
])
section(1,'after',[
 ['¡Oh, no! Qué derrota tan terrible. No podré detenerlos... pasa rápido antes de que alguien pregunte.','Oh no! Such a terrible defeat. I cannot stop you... hurry through before anyone asks.','Ah, não! Que derrota terrível. Não posso impedir... passem logo antes que alguém pergunte.','ああ、大変！ひどい負け方！もう止められない……誰かに聞かれる前に、早く行って。'],
 ['Gracias, Gabriel. Se lo contaré cuando despierte.','Thank you, Gabriel. I will tell him when he wakes.','Obrigada, Gabriel. Vou contar quando ele acordar.','ありがとう、ガブリエル。兄が起きたら伝えるね。'],
])
section(1,'outro',[
 ['No esperaba que su hermana quisiera traerlo al cielo. No me quedaba otra que hacerme la derrotada y dejarlos pasar. Una actuación impecable, si me preguntan.','I did not expect his sister to bring him to heaven. I had to pretend to lose and let them through. Flawless acting, if you ask me.','Não esperava que a irmã fosse trazê-lo ao céu. Tive que fingir a derrota e deixar passar. Uma atuação impecável, na minha opinião.','妹が彼を天界へ連れてくるとはね。負けたふりをして通すしかなかった。なかなかの名演技だったでしょ。'],
 ['Espero que por fin me invite esa comida que me prometió ese pollo estafador. Lleva pendiente desde antes de que lo mandaran a la Tierra...','I hope that cheating chicken finally treats me to the meal he promised. He has owed me since before they sent him to Earth...','Espero que aquele frango caloteiro finalmente pague a comida que prometeu. Deve desde antes de mandarem ele para a Terra...','あの口先だけの鳥、今度こそ約束のご飯に誘ってよね。地上へ送られる前から、ずっと待ってるんだから……'],
 ['Y no, no estoy sonriendo. Es la luz del cielo. La luz hace cosas raras con mi cara.','And no, I am not smiling. It is the heavenly light. It does strange things to my face.','E não, não estou sorrindo. É a luz do céu. Ela faz coisas estranhas com meu rosto.','別に、にやけてないし。天界の光のせい。この光、顔に変な作用があるんだよ。'],
])
section(2,'intro',[
 ['Otra alarma. Esta vez viene una locomotora con una boca enorme. ¿A quién se le ocurrió dejar entrar eso?','Another alarm. This time it is a train with an enormous mouth. Who let that in?','Outro alarme. Agora é uma locomotiva com uma boca enorme. Quem deixou isso entrar?','また警報。今度は巨大な口の機関車。誰があれを入れたの？'],
])
section(2,'before',[
 ['¡Eh! Este lugar no es un buffet.','Hey! This place is not a buffet.','Ei! Isto aqui não é um bufê.','ちょっと！ここは食べ放題じゃないよ。'],
 ['¡Entonces voy a probarlo todo!','Then I will try everything!','Então vou experimentar tudo!','なら、全部試してみる！'],
 ['Sí, claro. La comprensión lectora también está de vacaciones.','Of course. Reading comprehension is on holiday too.','Claro. A interpretação de texto também está de férias.','はいはい。読解力も休暇中なのね。'],
])
section(2,'after',[
 ['¡Fuera de mi estación! Y no muerdas el letrero al salir.','Out of my station! And do not bite the sign on your way out.','Fora da minha estação! E não morde a placa na saída.','私の駅から出ていって！帰りに看板を噛まないでよ。'],
])
section(2,'outro',[
 ['¿Ven a lo que me refiero? ¿Qué rayos era esa cosa? Hay días en que hasta extraño a los fanáticos que solo querían discutir.','See what I mean? What was that thing? Some days I even miss the fanatics who only wanted to argue.','Viram do que estou falando? Que coisa era aquela? Tem dia que sinto falta dos fanáticos que só queriam discutir.','わかったでしょ？何なの、あれ。口論するだけの信者が懐かしくなる日もあるよ。'],
 ['Aunque creo que el pollo ya dejó de comer con su padre y me ha estado buscando. Quién sabe... por él quizá podría pedir un día libre.','Though I think the chicken has finished eating with his father and has been looking for me. Who knows... I might ask for a day off for him.','Mas acho que o frango terminou de comer com o pai e anda me procurando. Quem sabe... por ele talvez peça uma folga.','でも、あの鳥、父親との食事は終わって、私を探してるみたい。どうしようかな……彼のためなら、休暇を申請してもいいかも。'],
 ['Llevo casi cinco años sin pedir ni uno. Seguro que el formulario ya ni se acuerda de mí.','I have not asked for one in almost five years. The form probably does not even remember me.','Faz quase cinco anos que não peço uma. O formulário nem deve lembrar de mim.','もう五年近く、一日も申請してないんだよ。申請書のほうが私を忘れてそう。'],
])
section(3,'intro',[
 ['No. No puede estar sonando otra vez. Hay una ReyMar acercándose... Gora-P, dime que trae a alguien de relevo.','No. That alarm cannot be ringing again. A ReyMar is approaching... Gora-P, tell me it is my relief.','Não. Esse alarme não pode tocar de novo. Uma ReyMar vem chegando... Gora-P, diz que é minha substituta.','嘘でしょ。また鳴るなんて。レイマールが来る……ゴラＰ、交代要員だって言って。'],
])
section(3,'before',[
 ['¡Dicen que aquí hay una guardia que nunca deja pasar una buena pelea!','I hear there is a guard here who never turns down a good fight!','Dizem que tem uma guarda aqui que nunca recusa uma boa luta!','ここには、いい勝負を断らない番人がいるって聞いたよ！'],
 ['No. Dicen que nunca me dejan terminar el turno. Es distinto.','No. They mean nobody lets my shift end. Different thing.','Não. Disseram que nunca deixam meu turno acabar. É diferente.','違う。「勤務を終わらせてもらえない番人」なの。全然違う。'],
 ['¡Entonces acabemos con una pelea memorable!','Then let us finish with a memorable fight!','Então vamos terminar com uma luta memorável!','なら、忘れられない勝負で締めよう！'],
])
section(3,'after',[
 ['¡Se acabó! No acepto revancha, encuesta de satisfacción ni otra visita.','It is over! No rematches, satisfaction surveys or further visits.','Acabou! Nada de revanche, pesquisa de satisfação ou nova visita.','終わり！再戦も、満足度アンケートも、再訪問も、お断り。'],
 ['¡Qué carácter! Me gusta.','What an attitude! I like it.','Que temperamento! Gostei.','いい気迫！気に入ったよ。'],
])
section(3,'outro',[
 ['¡Maldita loca! ¿Cómo hay gente que solo quiere pelear? Estoy cansada de todo esto. Hoy necesito algo que no intente conquistar, romper o morder el cielo.','That lunatic! How can anyone only want to fight? I am tired of all this. Today I need something that will not conquer, break or bite heaven.','Maluca! Como pode alguém só querer lutar? Cansei disso. Hoje preciso de algo que não tente conquistar, quebrar ou morder o céu.','あの戦闘狂！戦うことしか考えてないの？もう疲れた。今日は天界を征服したり壊したり噛んだりしないものが必要。'],
 ['Fue entonces cuando apareció el conductor de R-20. Mi pollo estafador. Dijo que estaba esperando a que terminara mi turno. Como si eso fuera a ocurrir por sí solo.','Then the R-20 driver appeared. My cheating chicken. He said he was waiting for my shift to end. As if that would happen on its own.','Foi quando apareceu o condutor da R-20. Meu frango caloteiro. Disse que esperava meu turno acabar. Como se isso fosse acontecer sozinho.','そこへＲ２０の機関士が来た。私の口先だけの鳥。「勤務が終わるのを待ってた」だって。放っておいて終わるとでも？'],
 ['Me contó que tardó un poco en regresar y bastante más en entender qué quería su padre para él. Pero hubo algo de lo que estuvo seguro todo el tiempo.','He said returning took a while, and understanding what his father wanted took longer. But he had been sure of one thing all along.','Disse que demorou para voltar e mais ainda para entender o que o pai queria dele. Mas sempre teve certeza de uma coisa.','帰ってくるのに時間がかかって、父親の望みを理解するのにはもっとかかったって。でも、ずっと確かなことが一つあったらしい。'],
 ['No quería volver solo para ver a su padre. Esta vez quería hacer las cosas bien y no dejarlo todo a la mitad. Aunque la primera vez lo mandaron a la Tierra, no es que él quisiera irse.','He had not wanted to return only for his father. This time he wanted to do things properly and not leave halfway through. Though last time he was sent to Earth; he had not chosen to leave.','Não queria voltar só pelo pai. Desta vez queria fazer direito e não deixar tudo pela metade. Mas antes o mandaram para a Terra; não foi porque quis ir embora.','父親に会うためだけに戻りたかったんじゃない。今度はちゃんと、途中で投げ出さずにやりたいって。まあ前は、自分から去ったんじゃなく、地上へ送られたんだけど。'],
 ['Yo sonreí. He oído que abrieron un maid café nuevo en el mundo humano. Se llama Baratito. Tal vez podrías llevarme en mi próximo día libre.','I smiled. I heard a new maid café opened in the human world. Baratito. Maybe you could take me on my next day off.','Eu sorri. Ouvi que abriram um maid café no mundo humano. Baratito. Talvez possa me levar na minha próxima folga.','私は笑った。「人間界に新しいメイドカフェができたって。バラティートっていうの。次の休みに、連れていってくれてもいいよ」。'],
 ['Solo espero que esta vez sí pague él. Y que, por una vez, la alarma pueda esperar.','I just hope he pays this time. And that, for once, the alarm can wait.','Só espero que desta vez ele pague. E que, por uma vez, o alarme possa esperar.','今度こそ彼が払いますように。そして一度くらい、警報が待ってくれますように。'],
])
add('gabriel.upgrade.weapon','Antes del siguiente turno: elige un arma','Before the next shift: choose a weapon','Antes do próximo turno: escolha uma arma','次の勤務へ：武器を選択')
add('gabriel.upgrade.passive','Ahora elige una mejora secundaria','Now choose a passive upgrade','Agora escolha uma melhoria passiva','次に補助強化を選択')
add('gabriel.upgrade.hint','Conservas tu tren y tus mejoras. Después comienza el siguiente duelo.','Keep your train and upgrades. The next duel starts afterwards.','Seu trem e melhorias continuam. Depois começa o próximo duelo.','列車と強化は維持されます。選択後、次の対決へ。')
add('gabriel.upgrade.repair','Equipo al máximo: reparación completa','Equipment maxed: full repair','Equipamento no máximo: reparo completo','装備最大：完全修理')
add('gabriel.upgrade.take','Mejorar: {0}','Upgrade: {0}','Melhorar: {0}','強化: {0}')
assert len({r['key'] for r in rows})==len(rows)
for r in rows:assert all(r.values()) and all('\t' not in v and '\n' not in v for v in r.values())
Path('Tools/gabriel_story_dialogues.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=Path('Assets/Localization/strings.tsv');lines=p.read_text(encoding='utf-8-sig').splitlines();updates={r['key']:'\t'.join(r[k] for k in ['key','es','en','pt','ja']) for r in rows}
for i,line in enumerate(lines):
 key=line.split('\t',1)[0]
 if key in updates:lines[i]=updates.pop(key)
lines.extend(updates.values());p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
Path('Tools/gabriel_story.md').write_text('# Gabriel\n\nGora-P; cielo, 30 minutos y suerte inicial +3. Onini → arma y pasiva → Tzmil → arma y pasiva → Giger Machine → arma y pasiva → ReyMar. Diálogos intermedios sobre la misma escena, conservando tren y equipo. Final romántico con el conductor de R-20 y Gracias. Modelo 3D de Gabriel pendiente; retrato ilustrado disponible.\n\n'+'\n\n'.join(r['key']+'\n'+r['es'] for r in rows)+'\n',encoding='utf-8')
print(f'{len(rows)} keys in four languages')
