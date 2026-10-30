import json
from pathlib import Path
rows=[]
def add(key,es,en,pt,ja): rows.append(dict(zip(['key','es','en','pt','ja'],[key,es,en,pt,ja])))
def section(c,side,lines):
    for n,line in enumerate(lines,1):add(f'story.giger.{c}.{side}.{n}',*line)
for c,line in enumerate([
 ['Hecha de locura','Made of madness','Feita de loucura','狂気から生まれて'],
 ['Una indigestión cósmica','Cosmic indigestion','Indigestão cósmica','宇宙の消化不良'],
 ['Mercancía frágil','Fragile goods','Mercadoria frágil','取扱注意'],
 ['El paraíso equivocado','The wrong paradise','O paraíso errado','間違った楽園'],
]):add(f'story.giger.{c}.title',*line)
add('hero.character.giger','Giger Machine','Giger Machine','Giger Machine','ギーガー・マシン')
section(0,'intro',[
 ['Soy Giger Machine. No me construyó un ingeniero ni me encargó ningún gobierno. Me creó la misma locura de este mundo. Eso explica bastantes cosas.','I am Giger Machine. No engineer built me and no government ordered me. The madness of this world created me. That explains quite a lot.','Sou Giger Machine. Nenhum engenheiro me construiu e nenhum governo me encomendou. A própria loucura deste mundo me criou. Isso explica muita coisa.','私はギーガー・マシン。技師が造ったのでも、政府が発注したのでもない。この世界の狂気そのものが私を生んだ。納得だろう。'],
 ['Mi única pasión es recorrer Pscoworld y destruir cualquier cosa de fuera que termine aquí. Algunos coleccionan recuerdos. Yo los mastico.','My only passion is roaming Pscoworld and destroying anything foreign that ends up here. Some collect souvenirs. I chew them.','Minha única paixão é percorrer Pscoworld e destruir tudo de fora que acaba aqui. Alguns colecionam lembranças. Eu mastigo.','プスコワールドを走り、外から落ちてきたものを壊す。それが生きがい。思い出を集める者もいる。私は噛む。'],
 ['Aquí nunca faltan visitantes confundidos, cajas que nadie reclama y cosas que no deberían tener ojos. Es un lugar encantador.','There is no shortage of confused visitors, unclaimed crates and things that should not have eyes. A charming place.','Não faltam visitantes confusos, caixas abandonadas e coisas que não deveriam ter olhos. Um lugar encantador.','困惑する来客、持ち主不明の箱、目があってはいけないもの。ここには何でもある。素敵な場所だ。'],
 ['Hoy apareció una chica con pelo de tentáculos y una locomotora con dientes. Me gusta su idea, aunque le faltan unos cuantos. Voy a darle la bienvenida.','Today a girl with tentacle hair arrived in a train with teeth. I like her idea, though she could use a few more. Time to welcome her.','Hoje apareceu uma garota com cabelo de tentáculos e uma locomotiva com dentes. Gostei da ideia, mas faltam alguns. Vou dar as boas-vindas.','今日は触手の髪の娘が、歯のある機関車で来た。悪くないが、歯が足りない。歓迎してやろう。'],
])
section(0,'before',[
 ['¡Otra visita! ¿Tu locomotora también mastica?','Another visitor! Does your train chew too?','Outra visita! Sua locomotiva também mastiga?','また来客だ！その機関車も噛むのか？'],
 ['¿Esto es la ciudad? El pollo no mencionó las vías con ojos.','Is this the city? The chicken never mentioned tracks with eyes.','Esta é a cidade? O frango não falou de trilhos com olhos.','ここが街？あの鳥、目のある線路なんて言ってなかった。'],
 ['No, pero puedo hacer que veas las estrellas.','No, but I can make you see stars.','Não, mas posso fazer você ver estrelas.','違うが、星なら見せてやれる。'],
])
section(0,'after',[
 ['¡Solo quería llegar a la ciudad! Esa salida parece bastante menos peligrosa.','I only wanted to reach the city! That exit looks a lot less dangerous.','Só queria chegar à cidade! Aquela saída parece bem menos perigosa.','街に行きたかっただけなのに！あの出口なら安全そう。'],
 ['Siempre dicen eso antes de cruzar. Es mi parte favorita.','They always say that before crossing. My favourite part.','Sempre dizem isso antes de atravessar. É minha parte favorita.','渡る前は皆そう言う。一番好きな瞬間だ。'],
])
section(0,'outro',[
 ['Lo mejor de este mundo es que quienes caen aquí siempre terminan en un lugar peor. Me daban tantas ganas de conocer esos sitios y causar un poquito de terror.','The best thing about this world is that everyone who falls in ends up somewhere worse. I always wanted to see those places and cause a little terror.','O melhor deste mundo é que quem cai aqui sempre acaba em um lugar pior. Sempre quis conhecer esses lugares e causar um pouco de terror.','ここに落ちた者は、必ずもっとひどい場所へ行く。それが最高だ。その場所で恐怖を振りまいてみたかった。'],
 ['Me distraje viendo a la chica pulpo cruzar a otro mundo. Una roca verde cayó de su locomotora. Pensé que sería un buen aperitivo y la mordí.','I was distracted watching the tentacle girl cross into another world. A green rock fell from her train. I thought it would make a nice snack and bit it.','Me distraí vendo a garota polvo passar para outro mundo. Uma pedra verde caiu da locomotiva dela. Achei que daria um bom petisco e mordi.','触手の娘が門を渡るのを見ていたら、機関車から緑の石が落ちた。おやつにいいと思って噛んだ。'],
 ['Resultó ser bastante explosiva. Salí volando directo al portal. ¡Al fin unas vacaciones! Y este lugar nuevo tiene muchísimas cosas por romper.','It was rather explosive. I flew straight into the portal. A holiday at last! And this new place has so much to break.','Era bastante explosiva. Voei direto para o portal. Enfim, férias! E este lugar novo tem muita coisa para quebrar.','なかなか爆発的な味だった。そのまま門へ吹き飛ばされた。ついに休暇！新しい場所には、壊すものが山ほどある。'],
])
section(1,'intro',[
 ['Todo es verde, húmedo y extrañamente blando. Creo que estoy dentro de algo enorme. Mejor no preguntar por dónde queda la salida.','Everything is green, damp and strangely soft. I think I am inside something enormous. Better not ask where the exit is.','Tudo é verde, úmido e estranhamente mole. Acho que estou dentro de algo enorme. Melhor não perguntar onde fica a saída.','緑で、湿っていて、妙に柔らかい。巨大な何かの中らしい。出口がどこかは聞かないでおこう。'],
 ['Una locomotora tejida viene a detenerme. Parece muy bien cuidada. Qué lástima que yo no haya venido a cuidar nada.','A knitted locomotive is coming to stop me. It looks very well cared for. Too bad I did not come here to care for anything.','Uma locomotiva de crochê vem me deter. Parece muito bem cuidada. Pena que não vim cuidar de nada.','編み物の機関車が止めに来た。大事にされているようだ。残念ながら、私は大事にするために来たのではない。'],
])
section(1,'before',[
 ['¿Todo esto se puede romper?','Can all this be broken?','Dá para quebrar tudo isso?','これは全部壊していいのか？'],
 ['No. Y vas a dejar de intentarlo.','No. And you are going to stop trying.','Não. E vai parar de tentar.','駄目だ。もうやめてもらう。'],
 ['¡Entonces sí se puede!','So it can!','Então dá!','つまり、壊せるんだな！'],
])
section(1,'after',[
 ['Thulu no está disfrutando tu visita.','Thulu is not enjoying your visit.','Thulu não está gostando da sua visita.','トゥルーは君の訪問を喜んでいない。'],
 ['¡Pero si apenas estaba empezando!','But I was only getting started!','Mas eu estava só começando!','まだ始めたばかりなのに！'],
])
section(1,'outro',[
 ['Al parecer, a ese lugar verde y extraño no le gustó nada el caos que llevaba conmigo. Sentí cómo todo se retorcía a mi alrededor.','Apparently that strange green place did not like the chaos I brought. I felt everything twisting around me.','Parece que aquele lugar verde e estranho não gostou do caos que levei. Senti tudo se contorcendo ao redor.','あの奇妙な緑の場所は、私の騒ぎが気に入らなかったらしい。周りがぐにゃりと縮んだ。'],
 ['Y después me escupió. Literalmente. Aterricé en otro sitio que pronto aprendí que llamaban infierno. Tiene un nombre prometedor.','Then it spat me out. Literally. I landed somewhere I soon learned was called hell. A promising name.','Depois me cuspiu. Literalmente. Caí num lugar que logo descobri que chamavam de inferno. Nome promissor.','そして吐き出された。文字通りだ。落ちた先は地獄という場所だった。期待できる名前だ。'],
])
section(2,'intro',[
 ['Fuego, cadenas y diablitos nerviosos. Por fin alguien entiende cómo decorar un lugar.','Fire, chains and nervous little devils. Finally someone understands decoration.','Fogo, correntes e diabinhos nervosos. Enfim alguém entende de decoração.','炎、鎖、落ち着きのない小悪魔。ようやく飾りつけのわかる場所に来た。'],
 ['Los diablitos intentaron apartarme de su mercancía. Grave error: si algo no quieren que toque, seguro es lo más divertido de romper.','The little devils tried to keep me away from their goods. Big mistake: anything they do not want me touching must be the most fun to break.','Os diabinhos tentaram me afastar da mercadoria. Grande erro: se não querem que eu toque, deve ser a coisa mais divertida de quebrar.','小悪魔たちは商品に近づけまいとした。大失敗だ。触らせたくないものほど、壊すと楽しそうだからな。'],
])
section(2,'before',[
 ['¡Acelerar, girar y romper! Eso sí que es un trabajo a mi medida.','Accelerate, steer and smash! Now that is my kind of job.','Acelerar, virar e quebrar! Esse trabalho é a minha cara.','加速、旋回、破壊！まさに私向きの仕事だ。'],
 ['¡No hace falta que griten! Ya vi por dónde guardan sus cosas.','No need to shout! I can already see where you keep your things.','Não precisam gritar! Já vi onde guardam suas coisas.','そんなに叫ばなくても！大事なものの置き場所は、もう見えたぞ。'],
])
section(2,'after',[
 ['¡Entrega completada! Ahora ocupan muchísimo menos espacio.','Delivery complete! They take up much less space now.','Entrega concluída! Agora ocupam muito menos espaço.','配達完了！ずいぶん省スペースになったな。'],
])
section(2,'outro',[
 ['Parece que a los diablitos tampoco les gustó tenerme de visita. Abrieron un portal y me pidieron amablemente que me fuera. Bueno, gritando.','The little devils did not enjoy my visit either. They opened a portal and politely asked me to leave. Well, by screaming.','Os diabinhos também não gostaram da visita. Abriram um portal e pediram gentilmente que eu saísse. Bom, gritando.','小悪魔たちも私の訪問が嫌だったらしい。門を開いて、丁寧に退去を頼んだ。叫びながら。'],
 ['Me mandaron al primer mundo que encontraron. Por las risas, creo que acababan de enviarles el problema a sus mayores enemigos. Qué considerados.','They sent me to the first world they found. Judging by the laughter, they had passed the problem to their greatest enemies. How thoughtful.','Me mandaram para o primeiro mundo que acharam. Pelas risadas, passaram o problema para os maiores inimigos deles. Que gentileza.','最初に見つけた世界へ送られた。あの笑い声からすると、最大の敵へ厄介者を押しつけたのだろう。親切だ。'],
])
section(3,'intro',[
 ['Nubes, puertas doradas y un silencio insoportable. Qué lugar tan delicado. No debería costarme mucho mejorarlo.','Clouds, golden gates and unbearable silence. Such a delicate place. Improving it should be easy.','Nuvens, portões dourados e um silêncio insuportável. Que lugar delicado. Deve ser fácil melhorar.','雲、金の門、耐えられない静けさ。ずいぶん繊細な場所だ。改善するのは簡単そうだ。'],
 ['Los ángeles enviaron a su mejor guerrero: Gora-P. Tiene cara de gato y va sobre raíles. Empiezo a entenderme con este lugar.','The angels sent their best warrior: Gora-P. A cat face on rails. This place is starting to make sense.','Os anjos mandaram seu melhor guerreiro: Gora-P. Cara de gato sobre trilhos. Estou começando a entender este lugar.','天使たちは最強の戦士ゴラＰを送り出した。線路を走る猫の顔。この場所もわかってきた。'],
])
section(3,'before',[
 ['¿Eres lo mejor que tienen? ¡Qué ganas de comprobarlo!','You are their best? I cannot wait to find out!','Você é o melhor deles? Mal posso esperar para testar!','お前が最強か？試すのが楽しみだ！'],
 ['Hasta aquí llegó tu paseo.','Your little trip ends here.','Seu passeio termina aqui.','お散歩はここまでだ。'],
 ['¡Eso me dijeron en los otros dos sitios!','That is what the last two places said!','Foi o que disseram nos outros dois lugares!','前の二か所でもそう言われた！'],
])
section(3,'after',[
 ['Esto no puede seguir así...','This cannot go on...','Isso não pode continuar...','このままにはできない……'],
 ['¡Ni su mejor guerrero pudo detener mi destrucción!','Even their best warrior could not stop my destruction!','Nem o melhor guerreiro parou minha destruição!','最強の戦士でも、私の破壊を止められなかった！'],
])
section(3,'outro',[
 ['Ni su mejor guerrero pudo pararme. Ya estaba pensando qué romper después cuando se acercó uno de esos ángeles.','Even their best warrior could not stop me. I was deciding what to break next when one of the angels approached.','Nem o melhor guerreiro me parou. Eu decidia o que quebrar depois quando um dos anjos chegou.','最強でも止められなかった。次は何を壊そうか考えていると、天使が一人近づいてきた。'],
 ['Me habló de un mundo donde podría destruir cosas para siempre, sin que nadie me echara. Sonaba demasiado bueno. Así que crucé el portal sin preguntar nada.','It told me of a world where I could destroy things forever without being thrown out. It sounded too good. So I crossed without asking a thing.','Falou de um mundo onde eu poderia destruir para sempre sem ser expulsa. Parecia bom demais. Atravessei sem perguntar nada.','追い出されず、永遠に壊せる世界があると言った。うますぎる話だ。だから何も聞かずに門を渡った。'],
 ['Era Pscoworld. Mi propio mundo. Me habían engañado. Reconocí hasta la seta torcida que siempre muerdo al pasar.','It was Pscoworld. My own world. I had been tricked. I even recognised the crooked mushroom I always bite on the way past.','Era Pscoworld. Meu próprio mundo. Fui enganada. Reconheci até o cogumelo torto que sempre mordo ao passar.','プスコワールドだった。故郷だ。騙された。通るたびに噛む曲がったキノコまで見覚えがある。'],
 ['Pero pronto entendí que este sí era mi lugar. Aquí podía romper lo que quisiera. Y siempre terminaba llegando algo nuevo de otro mundo.','But I soon understood this really was my place. Here I could break anything I wanted. And something new always arrived from another world.','Mas logo entendi que este era meu lugar. Aqui podia quebrar o que quisesse. E sempre chegava algo novo de outro mundo.','でも、こここそ居場所だと気づいた。何でも壊せるし、別の世界から新しいものがやって来る。'],
 ['Sobre todo visitantes nuevos. Esa cara de confusión cuando me ven acercarme... ¡Eso es lo que más me gusta!','Especially new visitors. That look of confusion when they see me coming... That is my favourite part!','Principalmente visitantes novos. A cara de confusão quando me veem chegando... É disso que mais gosto!','特に新しい来客。近づく私を見た時の、あの困った顔……それが何より大好きだ！'],
])
add('giger.crates.controls','Rompe al menos 35 de las 40 cajas. R2 / W / ↑: acelerar · Stick / A-D: dirigir · X / Shift: turbo · □ / S / L2: frenar y derrapar. Tienes 3 minutos. Las cajas no detienen tu tren.','Smash at least 35 of 40 crates. R2 / W / ↑: accelerate · Stick / A-D: steer · X / Shift: turbo · Square / S / L2: brake and drift. You have 3 minutes. Crates do not stop your train.','Quebre pelo menos 35 de 40 caixas. R2 / W / ↑: acelerar · Analógico / A-D: virar · X / Shift: turbo · Quadrado / S / L2: frear e derrapar. Você tem 3 minutos. Caixas não param seu trem.','四十個中三十五個以上の箱を壊そう。R2 / W / ↑：加速、スティック / A-D：方向、X / Shift：ターボ、□ / S / L2：ブレーキとドリフト。制限三分。箱に当たっても列車は止まらない。')
add('giger.crates.count','CAJAS: {0} / {1}','CRATES: {0} / {1}','CAIXAS: {0} / {1}','破壊：{0} / {1}')
add('giger.crates.target','OBJETIVO: {0} DE {1}','TARGET: {0} OF {1}','META: {0} DE {1}','目標：{1}個中{0}個')
add('giger.crates.won','¡DESTROZO COMPLETADO! {0} / {1}','SMASHING COMPLETE! {0} / {1}','DESTRUIÇÃO CONCLUÍDA! {0} / {1}','破壊完了！ {0} / {1}')
add('giger.crates.lost','FALTARON CAJAS: {0} / {1}','NOT ENOUGH CRATES: {0} / {1}','FALTARAM CAIXAS: {0} / {1}','箱が足りない：{0} / {1}')
assert len({r['key'] for r in rows})==len(rows)
for r in rows:assert all(r.values()) and all('\t' not in v and '\n' not in v for v in r.values())
Path('Tools/giger_story_dialogues.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=Path('Assets/Localization/strings.tsv');lines=p.read_text(encoding='utf-8-sig').splitlines();updates={r['key']:'\t'.join(r[k] for k in ['key','es','en','pt','ja']) for r in rows}
for i,line in enumerate(lines):
    key=line.split('\t',1)[0]
    if key in updates:lines[i]=updates.pop(key)
lines.extend(updates.values());p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
Path('Tools/giger_story.md').write_text('# Giger Machine\n\nPscoworld (Culu) → Thulu (Crochtrain) → infierno (35/40 cajas, controles de carrera) → cielo (Gora-P) → Pscoworld y Gracias.\n\nBoceto principiante: papel blanco, línea negra insegura, construcción roja/azul, sin rellenos ni fondos detallados.\n\n'+'\n\n'.join(r['key']+'\n'+r['es'] for r in rows)+'\n',encoding='utf-8')
print(f'{len(rows)} keys in 4 locales')
